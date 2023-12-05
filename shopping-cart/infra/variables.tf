variable "name" {
  type    = string
  default = "congo"
}

variable "location" {
  type    = string
  default = "ukwest"
}

output "name" {
  value = var.name
}
