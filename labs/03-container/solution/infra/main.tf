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

# --- Container Apps

resource "azurerm_log_analytics_workspace" "main" {
  name                = "log-${var.name}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
}

resource "azurerm_container_app_environment" "main" {
  name                       = "cae-${var.name}"
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
}

resource "azurerm_container_app" "api" {
  name                         = "ca-${var.name}-api"
  resource_group_name          = azurerm_resource_group.main.name
  container_app_environment_id = azurerm_container_app_environment.main.id
  revision_mode                = "Single"

  identity {
    type         = "UserAssigned"
    identity_ids = [azurerm_user_assigned_identity.app.id]
  }

  registry {
    server   = azurerm_container_registry.main.login_server
    identity = azurerm_user_assigned_identity.app.id
  }

  ingress {
    external_enabled = true
    target_port      = 8080

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0
    max_replicas = 2

    container {
      name   = "api"
      image  = "${azurerm_container_registry.main.login_server}/api:${var.image_tag}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "Storage__BlobServiceUri"
        value = azurerm_storage_account.main.primary_blob_endpoint
      }
      env {
        name  = "Storage__Container"
        value = azurerm_storage_container.documents.name
      }
      # Without this, DefaultAzureCredential does not know which of several identities to use
      env {
        name  = "AZURE_CLIENT_ID"
        value = azurerm_user_assigned_identity.app.client_id
      }
    }
  }

  # Roles must be in place before the app pulls the image and talks to storage
  depends_on = [
    azurerm_role_assignment.app_acr_pull,
    azurerm_role_assignment.app_blob,
  ]
}
