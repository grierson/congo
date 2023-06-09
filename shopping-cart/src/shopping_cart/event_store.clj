(ns shopping-cart.event-store
  (:require [next.jdbc :as jdbc]
            [next.jdbc.sql :as sql]))

(defn make-store []
  (let [db {:dbtype "h2" :dbname "shopping-cart"}
        ds (jdbc/get-datasource db)]
    (jdbc/execute!
     ds
     ["create table if not exists events (
      id UUID NOT NULL DEFAULT random_uuid() PRIMARY KEY,
      position int auto_increment,
      type varchar(32),
      data varchar (255),
      timestamp datetime default CURRENT_TIMESTAMP)"])
    ds))

(defn kill-store [store]
  (jdbc/execute! store ["drop table events"]))

(defn get-events
  ([store]
   (get-events store 1 10))
  ([store start end]
   (sql/query store ["SELECT * FROM events WHERE position BETWEEN ? AND ?" start end])))

(defn raise
  [store type data]
  (sql/insert! store :events {:type type :data (str data)}))
