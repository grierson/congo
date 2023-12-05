# congo/shopping-cart

Local

```
bb uber:build
bb uber:run
```

Local Docker

```
bb docker:build
bb docker:run
```

## Azure cloud

```
# Rename app within infra/varabiles for your own name

# Create Azure infrastructure
bb infra:apply
# Login into your Azure Container registery
bb acr:login
# Build + Tag + Push image to you ACR
bb acr:push
# Add Azure k8 to kubectl
bb k8:connect
# Apply image to your Azure K8
bb k8:apply
# Get k8 external IP
bb k8:show
```

Access running service

```
# Check k8 is running
http://<external-ip>:5000/health
```

## Azure destroy

```
bb k8:delete
bb infra:destroy
```
