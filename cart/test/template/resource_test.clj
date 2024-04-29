(ns template.resource-test
  (:require
   [clojure.test :refer [deftest is testing use-fixtures]]
   [donut.system :as ds]
   [halboy.navigator :as navigator]
   [halboy.resource :as hal]
   [next.jdbc :as jdbc]
   [placid-fish.core :as uris]
   [template.helper :as helper]
   [template.system]))

(require 'hashp.core)

(def test-system (atom nil))

(use-fixtures
  :once
  (fn [tests]
    (let [system (ds/start ::helper/test)
          _ (reset! test-system system)]
      (tests)
      (ds/stop system))))

(defn clear-tables [store]
  (jdbc/execute! store ["TRUNCATE TABLE events"])
  (jdbc/execute! store ["TRUNCATE TABLE projections"]))

(use-fixtures :each
  (fn [test]
    (test)
    (let [database (get-in @test-system [::ds/instances :components :database])]
      (clear-tables database))))

(defn extract [system]
  (let [host (get-in system [::ds/instances :env :webserver :host])
        port (get-in system [::ds/instances :env :webserver :port])]
    {:address (str "http://" host ":" port)
     :handler (get-in system [::ds/instances :http :handler])
     :database (get-in system [::ds/instances :components :database])}))

(deftest discovery-test
  (let [{:keys [address]} (extract @test-system)
        navigator (navigator/discover address)
        resource (navigator/resource navigator)]

    (testing "has self link"
      (let [self-href (hal/get-href resource :self)]
        (is (uris/absolute? self-href))
        (is (uris/ends-with? self-href "/"))))

    (testing "has health link"
      (let [health-href (hal/get-href resource :health)]
        (is (uris/absolute? health-href))
        (is (uris/ends-with? health-href "/health"))))

    (testing "has events link"
      (let [events-href (hal/get-href resource :events)]
        (is (uris/absolute? events-href))
        (is (uris/ends-with? events-href "/events{?start,end}"))))

    (testing "has cart link"
      (let [cart-href (hal/get-href resource :cart)]
        (is (uris/absolute? cart-href))
        (is (uris/ends-with? cart-href "/cart/{id}"))))

    (testing "has carts link"
      (let [carts-href (hal/get-href resource :carts)]
        (is (uris/absolute? carts-href))
        (is (uris/ends-with? carts-href "/carts"))))))

(deftest health-test
  (let [{:keys [address]} (extract @test-system)
        navigator (-> (navigator/discover address)
                      (navigator/get :health))
        resource (navigator/resource navigator)]

    (testing "Calling health returns 200"
      (is (= 200 (navigator/status navigator))))

    (testing "has self link"
      (let [self-href (hal/get-href resource :self)]
        (is (uris/absolute? self-href))
        (is (uris/ends-with? self-href "/health"))))

    (testing "has discovery link"
      (let [discovery-href (hal/get-href resource :discovery)]
        (is (uris/absolute? discovery-href))
        (is (uris/ends-with? discovery-href "/"))))

    (testing "has health property"
      (let [status (hal/get-property resource :status)]
        (is (= "healthy" status))))))

(deftest events-test
  (let [{:keys [address]} (extract @test-system)
        navigator (-> (navigator/discover address)
                      (navigator/get :events))
        resource (navigator/resource navigator)]

    (testing "Calling events returns 200"
      (is (= 200 (navigator/status navigator))))

    (testing "Contains events"
      (is (= 0 (count (hal/get-property resource :events)))))))

(deftest get-event-test
  (let [{:keys [address]} (extract @test-system)
        name "alice"
        _  (-> (navigator/discover address)
               (navigator/post :carts {:name name}))
        navigator (-> (navigator/discover address)
                      (navigator/get :events))
        resource (navigator/resource navigator)
        events (hal/get-resource resource :events)
        first-event (first events)

        first-event-href (hal/get-href first-event :self)
        event-navigator (navigator/discover first-event-href)
        event-resource (navigator/resource event-navigator)]
    (testing "Calling GET returns 200"
      (is (= 200 (navigator/status event-navigator))))
    (testing "properties"
      (is (= name (hal/get-property event-resource :name)))
      (is (= "cart-created" (hal/get-property event-resource :type))))))

(deftest post-carts-test
  (let [{:keys [address]} (extract @test-system)
        name "alice"
        navigator (-> (navigator/discover address {:follow-redirects false})
                      (navigator/post :carts {:name name}))
        resource  (navigator/resource navigator)]
    (testing "Calling POST returns 201"
      (is (= 201 (navigator/status navigator))))
    (testing "properties"
      (is (= name (hal/get-property resource :name))))))

(deftest get-cart-test
  (let [{:keys [address]} (extract @test-system)
        name "alice"
        navigator  (-> (navigator/discover address)
                       (navigator/post :carts {:name name}))
        resource (navigator/resource navigator)]
    (testing "Calling GET returns 200"
      (is (= 200 (navigator/status navigator))))
    (testing "properties"
      (is (= name (hal/get-property resource :name))))))

(deftest get-carts-test
  (let [{:keys [address]} (extract @test-system)
        [name1 name2] ["alice" "bob"]
        _ (-> (navigator/discover address)
              (navigator/post :carts {:name name1}))
        _ (-> (navigator/discover address)
              (navigator/post :carts {:name name2}))
        navigator (-> (navigator/discover address)
                      (navigator/get :carts))
        resource (navigator/resource navigator)]
    (testing "returns 200"
      (is (= 200 (navigator/status navigator))))
    (testing "properties"
      (is (= 2 (count (hal/get-property resource :carts)))))))
