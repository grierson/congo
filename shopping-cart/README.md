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

ACR Docker

```
cd infra/
terraform apply
cd ..
bb acr:login
bb docker:build
bb acr:tag
bb acr:push
bb k8:apply
```
