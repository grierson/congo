(ns congo.shopping-cart
  (:require [taoensso.carmine :as car :refer [wcar]]))

(defn get-cart [store id]
  (if-let [cart (wcar store (car/get id))]
    cart
    {:user-id id
     :items []}))

(defn add-items [cart items]
  (update cart :items concat items))

(defn delete-items [cart ids-to-remove]
  (let [ids-to-remove (set ids-to-remove)]
    (update cart :items #(remove (fn [item] (contains? ids-to-remove (:product-catalog-id item))) %))))

(defn save-cart [store {:keys [user-id] :as cart}]
  (wcar store (car/set user-id cart)))
