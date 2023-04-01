(ns congo.resource
  (:require
   [reitit.ring :as ring]
   [reitit.coercion.malli :as mcoercion]
   [reitit.ring.coercion :as rrc]
   [reitit.ring.middleware.parameters :as parameters]
   [reitit.ring.middleware.muuntaja :as muuntaja]
   [muuntaja.core :as m]
   [taoensso.carmine :as car :refer [wcar]]))

(defn app
  [store]
  (ring/ring-handler
   (ring/router
    [["/health" {:get {:handler (fn [_]
                                  {:status 200
                                   :body "healthy"})}}]
     ["/shoppingcart"
      ["/:id" {:get {:parameters {:path {:id int?}}
                     :handler (fn [{{{:keys [id]} :path} :parameters}]
                                (let [cart (wcar store (car/get id))]
                                  (if cart
                                    {:status 200
                                     :body cart}
                                    (do
                                      (let [new-cart {:user-id id
                                                      :items []}]
                                        (wcar store (car/set id new-cart))
                                        {:status 200
                                         :body new-cart})))))}}]]]
    {:data       {:coercion mcoercion/coercion
                  :muuntaja   m/instance
                  :middleware [parameters/parameters-middleware
                               muuntaja/format-negotiate-middleware
                               muuntaja/format-request-middleware
                               muuntaja/format-response-middleware
                               rrc/coerce-request-middleware
                               rrc/coerce-response-middleware]}})))
