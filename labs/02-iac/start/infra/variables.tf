variable "location" {
  description = "Azure region for all resources"
  type        = string
  default     = "switzerlandnorth"
}

variable "name" {
  description = "Short name used as prefix for all resources"
  type        = string
  default     = "docplatform"
}
