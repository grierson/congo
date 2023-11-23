(ns shopping-cart.product-catalog)

(defn make-store []
  (let [product-id-1 1
        product-id-2 2]
    {product-id-1
     {:product-catalog-id product-id-1
      :product-name "t-shirt"}
     product-id-2
     {:product-catalog-id product-id-2
      :product-name "pants"}}))

(defn get-products [gateway product-ids]
  (vals (select-keys gateway product-ids)))
