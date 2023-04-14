(ns congo.product-catalog)

(defn make-store []
  (let [product-id-1 1
        product-name-1 "t-shirt"
        product-id-2 2
        product-name-2 "pants"
        t-shirt {:product-catalog-id product-id-1
                 :product-name product-name-1}
        pants {:product-catalog-id product-id-2
               :product-name product-name-2}]
    {product-id-1 t-shirt
     product-id-2 pants}))

(defn get-products [gateway product-ids]
  (vals (select-keys gateway product-ids)))
