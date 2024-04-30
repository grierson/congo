(ns template.core
  (:require
   [donut.system :as ds]
   [next.jdbc :as jdbc]
   [template.audit :as audit]
   [template.helper :as helper]
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
  (projection/create-projection! database {:sku 1
                                           :name "shirt"
                                           :description "two sleves"
                                           :price 10})
  (projection/create-projection! database {:sku 2
                                           :name "pants"
                                           :description "two legs"
                                           :price 20})
  (audit/get-events database)
  (audit/get-projections database)
  (jdbc/execute! database ["SELECT * FROM projections WHERE (data->>'sku')::integer = ANY(?)" (int-array [1 2])])
  (audit/get-projections-by-id database [1 2])
  (+ 1 1))
