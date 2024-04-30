(ns template.core
  (:require [template.helper :as helper]
            [donut.system :as ds]
            [template.audit :as audit]
            [template.product.projection :as projection])
  (:gen-class))

(defn -main
  []
  (println "running...")
  (let [system (ds/start ::helper/test)
        database (get-in system [::ds/instances :components :database])]
    (audit/raise-event database (projection/product-created-event {:data {:sku 1
                                                                          :name "shirt"
                                                                          :description "two sleves"
                                                                          :price 10}}))
    (audit/raise-event database (projection/product-created-event {:data {:sku 2
                                                                          :name "pants"
                                                                          :description "two legs"
                                                                          :price 20}}))))

(comment
  (def system (ds/start ::helper/test))
  (def database (get-in system [::ds/instances :components :database]))
  (audit/raise-event database (projection/product-created-event {:data {:sku 1
                                                                        :name "shirt"
                                                                        :description "two sleves"
                                                                        :price 10}}))
  (audit/raise-event database (projection/product-created-event {:data {:sku 2
                                                                        :name "pants"
                                                                        :description "two legs"
                                                                        :price 20}}))
  (audit/get-events database))
