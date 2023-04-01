(ns congo.resource
  (:require
   [reitit.ring :as ring]
   [malli.experimental.lite :as l]
   [reitit.coercion.malli :as mcoercion]
   [reitit.ring.coercion :as rrc]
   [reitit.ring.middleware.parameters :as parameters]
   [reitit.ring.middleware.muuntaja :as muuntaja]
   [muuntaja.core :as m]
   [congo.shopping-cart :as shopping-cart]
   [congo.product-catalog :as product-catalog]))

(defn app
  [shopping-cart-store product-catalog-gateway]
  (ring/ring-handler
   (ring/router
    [["/health" {:get {:handler (fn [_]
                                  {:status 200
                                   :body "healthy"})}}]
     ["/shoppingcart"
      ["/:id" {:get {:parameters {:path {:id int?}}
                     :handler (fn [{{{:keys [id]} :path} :parameters}]
                                (let [cart (shopping-cart/get-cart shopping-cart-store id)]
                                  {:status 200
                                   :body cart}))}}]
      ["/:id/items" {:post {:parameters {:path {:id int?}
                                         :body {:product-ids (l/vector int?)}}
                            :handler (fn [{:keys [parameters]}]
                                       (let [id (-> parameters :path :id)
                                             product-ids (-> parameters :body :product-ids)
                                             cart (shopping-cart/get-cart shopping-cart-store id)
                                             products (product-catalog/get-products product-catalog-gateway product-ids)
                                             new-cart (shopping-cart/add-items cart products)
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
