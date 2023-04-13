(ns congo.event-store)

(def counter (atom 0))

(defn make-store []
  (atom []))

(defn get-events [store start end]
  (subvec @store start end))

(defn now [] (new java.util.Date))

(defn raise [store event]
  (swap! store conj (merge  event {:timestamp (now) :order @counter}))
  (swap! counter inc))
