(ns shopping-cart.resource
  (:require
   [reitit.ring :as ring]
   [malli.experimental.lite :as l]
   [reitit.coercion.malli :as mcoercion]
   [reitit.ring.coercion :as rrc]
   [reitit.ring.middleware.parameters :as parameters]
   [reitit.ring.middleware.muuntaja :as muuntaja]
   [muuntaja.core :as m]
   [shopping-cart.shopping-cart :as shopping-cart]
   [shopping-cart.product-catalog :as product-catalog]
   [shopping-cart.event-store :as events]))

(defn app
  [{:keys [shopping-cart-store product-catalog-gateway event-store]}]
  (ring/ring-handler
   (ring/router
    [["/health" {:get
                 {:handler (fn [_]
                             {:status 200
                              :body "healthy"})}}]
     ["/events" {:get
                 {:parameters {:query {:start (l/optional int?)
                                       :end  (l/optional int?)}}
                  :handler (fn [{{{:keys [start end]
                                   :or {start 0
                                        end (+ start 10)}} :query} :parameters}]
                             {:status 200
                              :body (events/get-events event-store start end)})}}]
     ["/shoppingcart"
      ["/:id" {:get
               {:parameters {:path {:id int?}}
                :handler (fn [{{{:keys [id]} :path} :parameters}]
                           (let [cart (shopping-cart/get-cart shopping-cart-store id)]
                             {:status 200
                              :body cart}))}}]
      ["/:id/items" {:post
                     {:parameters {:path {:id int?}
                                   :body {:product-ids (l/vector int?)}}
                      :handler (fn [{:keys [parameters]}]
                                 (let [id (-> parameters :path :id)
                                       product-ids (-> parameters :body :product-ids)
                                       cart (shopping-cart/get-cart shopping-cart-store id)
                                       products (product-catalog/get-products product-catalog-gateway product-ids)
                                       new-cart (shopping-cart/add-items event-store cart products)
                                       _ (shopping-cart/save-cart shopping-cart-store new-cart)]
                                   {:status 200
                                    :body new-cart}))}
                     :delete
                     {:parameters {:path {:id int?}
                                   :body {:product-ids (l/vector int?)}}
                      :handler (fn [{:keys [parameters]}]
                                 (let [id (-> parameters :path :id)
                                       product-ids (-> parameters :body :product-ids)
                                       cart (shopping-cart/get-cart shopping-cart-store id)
                                       new-cart (shopping-cart/delete-items event-store cart product-ids)
                                       _ (shopping-cart/save-cart shopping-cart-store new-cart)]
                                   {:status 200
                                    :body new-cart}))}}]]]
    {:data       {:coercion mcoercion/coercion
                  :muuntaja   m/instance
                  :middleware [parameters/parameters-middleware
                               muuntaja/format-negotiate-middleware
                               muuntaja/format-request-middleware
                               muuntaja/format-response-middleware
                               rrc/coerce-request-middleware
                               rrc/coerce-response-middleware]}})))
