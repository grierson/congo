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
                                   :body "healthy"})}}]

     ["/shoppingcart"
      ["/:id" {:get {:parameters {:path {:id int?}}
                     :handler (fn [{{{:keys [id]} :path} :parameters}]
                                {:status 200
                                 :body {:user-id id}})}}]]]
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
   {:components {:shopping-cart-store #::ds{:start (fn [_] "start redis conn")
                                            :stop (fn [{:keys [::ds/instance]}]
                                                    (.stop instance))
                                            :config {:address 2000}}}

    :http {:handler
           #::ds{:start  (fn [{:keys [::ds/config]}] app)
                 :config {}}
           :server
           #::ds{:start (fn [{{:keys [handler port]} ::ds/config}]
                          (rj/run-jetty
                           handler
                           {:port  port
                            :join? false}))
                 :stop  (fn [{:keys [::ds/instance]}]
                          (.stop instance))
                 :config  {:handler (ds/local-ref [:handler])
                           :port 9000}}}}})

(comment
  (def running-system (ds/signal system ::ds/start))
  (ds/signal running-system ::ds/stop))

(defn -main
  "I don't do a whole lot ... yet."
  [& args]
  (app {:name (first args)}))

