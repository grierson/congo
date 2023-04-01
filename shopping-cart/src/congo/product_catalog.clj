(ns congo.product-catalog)

(defn get-products [gateway product-ids]
  (vals (select-keys gateway product-ids)))
