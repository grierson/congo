(ns shopping-cart.system
  (:require [donut.system :as ds]
            [ring.adapter.jetty :as rj]
            [aero.core :as aero]
            [shopping-cart.resource :as resource]
            [shopping-cart.event-store :as events]
            [shopping-cart.shopping-cart :as shopping-cart]
            [shopping-cart.product-catalog :as product-catalog]))

(defn env-config [& [profile]]
  (aero/read-config "config.edn" (when profile {:profile profile})))

(def base-system
  {::ds/defs
   {:env {}

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
                     :options {:port (ds/ref [:env :webserver :port])
                               :join? false}}}}}})

(defmethod ds/named-system ::base
  [_]
  base-system)

(defmethod ds/named-system ::production
  [_]
  (ds/system ::base {[:env] (env-config :production)}))

(defmethod ds/named-system ::test
  [_]
  (ds/system ::base {[:env] (env-config :test)
                     [:http :server] ::disabled}))

(comment
  (def system (ds/start ::production))
  (println (get-in system [::ds/instances :env]))
  (ds/stop system))

