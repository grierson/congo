(ns shopping-cart.core
  (:require [shopping-cart.system :as system]
            [donut.system :as ds])
  (:gen-class))

(defn -main
  []
  (println "running...")
  (ds/start ::system/production))
