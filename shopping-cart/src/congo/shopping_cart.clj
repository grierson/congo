(ns congo.shopping-cart
  (:gen-class)
  (:require
   [donut.system :as ds]
   [ring.adapter.jetty :as rj]
   [reitit.ring :as ring]
   [reitit.coercion.malli :as mcoercion]
   [reitit.ring.coercion :as rrc]
   [reitit.ring.middleware.parameters :as parameters]
   [reitit.ring.middleware.muuntaja :as muuntaja]
   [muuntaja.core :as m]))

(def app
  (ring/ring-handler
   (ring/router
    [["/health" {:get {:handler (fn [_]
                                  {:status 200
                                   :body "healthy"})}}]]
    {:data       {:coercion mcoercion/coercion
                  :muuntaja   m/instance
                  :middleware [parameters/parameters-middleware
                               muuntaja/format-negotiate-middleware
                               muuntaja/format-request-middleware
                               muuntaja/format-response-middleware
                               rrc/coerce-request-middleware
                               rrc/coerce-response-middleware]}})))

(def system
  {::ds/defs
   {:http
    {:server #::ds{:start (fn [{:keys [::ds/config]}]
                            (rj/run-jetty
                             app
                             {:port  (:port config)
                              :join? false}))
                   :stop  (fn [{:keys [::ds/instance]}]
                            (.stop instance))
                   :config  {:port 9000}}}}})

(comment
  (def running-system (ds/signal system ::ds/start))
  (ds/signal running-system ::ds/stop))

(defn -main
  "I don't do a whole lot ... yet."
  [& args]
  (app {:name (first args)}))

