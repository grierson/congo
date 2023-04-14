(ns congo.system
  (:require [donut.system :as ds]
            [ring.adapter.jetty :as rj]
            [congo.resource :as resource]
            [congo.event-store :as events]
            [congo.shopping-cart :as shopping-cart]
            [congo.product-catalog :as product-catalog]))

(def base-system
  {::ds/defs
   {:env {:http-port 9000}

    :components
    {:event-store
     #::ds{:start (fn [_] (events/make-store))
           :stop (fn [{:keys [::ds/instance]}]
                   (events/kill-store instance))}
     :shopping-cart-server
     #::ds {:start (fn [_] (shopping-cart/make-server))
            :stop (fn [{:keys [::ds/instance]}]
                    (.stop instance))
            :config {:address 2000}}

     :shopping-cart-store
     #::ds{:start (fn [{{:keys [shopping-cart-server]} ::ds/config}]
                    (shopping-cart/make-store shopping-cart-server))
           :config {:shopping-cart-server (ds/ref [:components :shopping-cart-server])}}

     :product-catalog-gateway
     #::ds {:start (product-catalog/make-store)}}

    :http
    {:handler
     #::ds{:start  (fn [{{:keys [shopping-cart-store
                                 product-catalog-gateway
                                 event-store]} ::ds/config}]
                     (resource/app {:shopping-cart-store shopping-cart-store
                                    :product-catalog-gateway product-catalog-gateway
                                    :event-store event-store}))
           :config {:shopping-cart-store (ds/ref [:components :shopping-cart-store])
                    :product-catalog-gateway (ds/ref [:components :product-catalog-gateway])
                    :event-store (ds/ref [:components :event-store])}}

     :server
     #::ds{:start (fn [{{:keys [handler options]} ::ds/config}]
                    (rj/run-jetty handler options))
           :stop  (fn [{:keys [::ds/instance]}]
                    (.stop instance))
           :config  {:handler (ds/local-ref [:handler])
                     :options {:port (ds/ref [:env :http-port])
                               :join? false}}}}}})

(defmethod ds/named-system ::base
  [_]
  base-system)

(defmethod ds/named-system ::test
  [_]
  (ds/system ::base {[:http :server] ::disabled}))

(comment
  (def system (ds/start ::test))
  (println (get-in system [::ds/instances :http :handler]))
  (ds/stop system))

