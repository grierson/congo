(ns congo.shopping-cart
  (:require
   [congo.event-store :as events]
   [taoensso.carmine :as car :refer [wcar]])
  (:import
   [com.github.fppt.jedismock RedisServer]))

(defn make-store []
  (let [server (RedisServer/newRedisServer)
        _ (.start server)
        redis-host (.getHost server)
        redis-port (.getBindPort server)
        redis-url (str "redis://" redis-host ":" redis-port)]
    {:pool (car/connection-pool {})
     :spec {:uri redis-url}}))

(defn get-cart [store id]
  (if-let [cart (wcar store (car/get id))]
    cart
    {:user-id id
     :items []}))

(defn add-items [event-store cart items]
  (dorun (map #(events/raise event-store "ShoppingCartItemAdded" %) items))
  (update cart :items concat items))

(defn delete-items [event-store cart ids-to-remove]
  (dorun (map #(events/raise event-store "ShoppingCartItemRemoved" %) ids-to-remove))
  (let [ids-to-remove (set ids-to-remove)]
    (update cart :items #(remove (fn [item] (contains? ids-to-remove (:product-catalog-id item))) %))))

(defn save-cart [store {:keys [user-id] :as cart}]
  (wcar store (car/set user-id cart)))
