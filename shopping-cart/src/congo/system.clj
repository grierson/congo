(ns congo.system
  (:require [donut.system :as ds]
            [ring.adapter.jetty :as rj]
            [congo.resource :as resource]
            [taoensso.carmine :as car :refer [wcar]])
  (:import (com.github.fppt.jedismock RedisServer)))

(def base-system
  {::ds/defs
   {:env {:http-port 9000}

    :components
    {:shopping-cart-store
     #::ds{:start (fn [_]
                    (let [server (RedisServer/newRedisServer)
                          _ (.start server)
                          redis-host (.getHost server)
                          redis-port (.getBindPort server)
                          redis-url (str "redis://" redis-host ":" redis-port)]
                      {:pool (car/connection-pool {})
                       :spec {:uri redis-url}}))
           :stop (fn [{:keys [::ds/instance]}]
                   (.stop instance))
           :config {:address 2000}}}

    :http
    {:handler
     #::ds{:start  (fn [{{:keys [store]} ::ds/config}] (resource/app store))
           :config {:store (ds/ref [:components :shopping-cart-store])}}

     :server
     #::ds{:start (fn [{{:keys [handler options]} ::ds/config}]
                    (rj/run-jetty handler options))
           :stop  (fn [{:keys [::ds/instance]}]
                    (.stop instance))
           :config  {:handler (ds/local-ref [:handler])
                     :options {:port (ds/ref [:env :http-port])
                               :join? false}}}}}})

(defmethod ds/named-system :base
  [_]
  base-system)

(defmethod ds/named-system :dev
  [_]
  (ds/system :base {[:env] "value"}))

(defmethod ds/named-system :test
  [_]
  (ds/system :dev {[:http :server] ::disabled}))

(comment
  (def system (ds/start :base))
  (ds/stop system))

