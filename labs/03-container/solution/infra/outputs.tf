output "registry_name" {
  value = azurerm_container_registry.main.name
}

output "registry_login_server" {
  value = azurerm_container_registry.main.login_server
}

output "storage_account_name" {
  value = azurerm_storage_account.main.name
}

output "resource_group" {
  value = azurerm_resource_group.main.name
}

output "app_url" {
  value = "https://${azurerm_container_app.api.ingress[0].fqdn}"
}
