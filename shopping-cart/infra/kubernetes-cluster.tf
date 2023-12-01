resource "azurerm_kubernetes_cluster" "cluster" {
  name                = var.app_name
  location            = var.location
  resource_group_name = azurerm_resource_group.resource_group.name
  dns_prefix          = var.app_name

  default_node_pool {
    name       = "default"
    node_count = 1
    vm_size    = "Standard_B2s"
  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_role_assignment" "role_assignment" {
  principal_id                     = azurerm_kubernetes_cluster.cluster.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.container_registry.id
  skip_service_principal_aad_check = true
}
