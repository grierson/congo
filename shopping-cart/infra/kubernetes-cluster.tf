resource "azurerm_kubernetes_cluster" "cluster" {
  name                = "griersonCongoAks"
  location            = azurerm_resource_group.congo.location
  resource_group_name = azurerm_resource_group.congo.name
  dns_prefix          = "griersonCongoAks"

  default_node_pool {
    name       = "default"
    node_count = 1
    vm_size    = "Standard_D2_v2"
  }

  identity {
    type = "SystemAssigned"
  }
}

resource "azurerm_role_assignment" "role_assignment" {
  principal_id                     = azurerm_kubernetes_cluster.cluster.kubelet_identity[0].object_id
  role_definition_name             = "AcrPull"
  scope                            = azurerm_container_registry.acr.id
  skip_service_principal_aad_check = true
}

resource "null_resource" "example" {
  provisioner "local-exec" {
    command = "az aks get-credentials --resource-group ${azurerm_resource_group.congo.name} --name griersonCongoAks"
  }
}
