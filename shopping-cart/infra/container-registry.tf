resource "azurerm_container_registry" "acr" {
  name                = "griersonCongoAcr"
  resource_group_name = azurerm_resource_group.congo.name
  location            = azurerm_resource_group.congo.location
  sku                 = "Basic"
  admin_enabled       = true
}
