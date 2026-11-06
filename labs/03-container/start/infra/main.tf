resource "random_string" "suffix" {
  length  = 5
  upper   = false
  special = false
}

locals {
  suffix = random_string.suffix.result
  # Storage and registry names must be globally unique and contain only lowercase letters and digits
  storage_name  = "st${var.name}${local.suffix}"
  registry_name = "acr${var.name}${local.suffix}"
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${var.name}"
  location = var.location
}

# --- Storage: no shared keys, no anonymous access. Only Entra ID (Managed Identity) gets in.

resource "azurerm_storage_account" "main" {
  name                     = local.storage_name
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  shared_access_key_enabled       = false
  allow_nested_items_to_be_public = false
  min_tls_version                 = "TLS1_2"
}

resource "azurerm_storage_container" "documents" {
  name                  = "dokumente"
  storage_account_id    = azurerm_storage_account.main.id
  container_access_type = "private"
}

# --- Registry: no admin user, the app pulls with its identity (AcrPull)

resource "azurerm_container_registry" "main" {
  name                = local.registry_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "Basic"
  admin_enabled       = false
}

# --- Identity of the app. User-assigned so that the roles exist before the app starts.

resource "azurerm_user_assigned_identity" "app" {
  name                = "id-${var.name}-app"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
}

resource "azurerm_role_assignment" "app_acr_pull" {
  scope                = azurerm_container_registry.main.id
  role_definition_name = "AcrPull"
  principal_id         = azurerm_user_assigned_identity.app.principal_id
}

# Scope is the single container, not the whole account
resource "azurerm_role_assignment" "app_blob" {
  scope                = azurerm_storage_container.documents.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_user_assigned_identity.app.principal_id
}
