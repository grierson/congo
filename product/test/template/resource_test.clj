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

    (testing "has product link"
      (let [product-href (hal/get-href resource :product)]
        (is (uris/absolute? product-href))
        (is (uris/ends-with? product-href "/product/{id}"))))

    (testing "has products link"
      (let [products-href (hal/get-href resource :products)]
        (is (uris/absolute? products-href))
        (is (uris/ends-with? products-href "/products"))))))

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
        product {:sku 1
                 :name "shirt"
                 :description "thing"
                 :price 10}
        _  (-> (navigator/discover address)
               (navigator/post :products product))
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
      (is (= (:sku product) (hal/get-property event-resource :sku)))
      (is (= (:name product) (hal/get-property event-resource :name)))
      (is (= (:description product) (hal/get-property event-resource :description)))
      (is (= (:price product) (hal/get-property event-resource :price)))
      (is (= "product-created" (hal/get-property event-resource :type))))))

(deftest post-products-test
  (let [{:keys [address]} (extract @test-system)
        product {:sku 1
                 :name "pants"
                 :description "two legs"
                 :price 10}
        navigator (-> (navigator/discover address {:follow-redirects false})
                      (navigator/post :products product))
        resource  (navigator/resource navigator)]
    (testing "Calling POST returns 201"
      (is (= 201 (navigator/status navigator))))
    (testing "properties"
      (is (= (:sku product) (hal/get-property resource :sku)))
      (is (= (:name product) (hal/get-property resource :name)))
      (is (= (:description product) (hal/get-property resource :description)))
      (is (= (:price product) (hal/get-property resource :price))))))

(deftest get-product-test
  (let [{:keys [address]} (extract @test-system)
        product {:sku 1
                 :name "shirt"
                 :description "two sleves"
                 :price 10}
        navigator  (-> (navigator/discover address)
                       (navigator/post :products product))
        resource (navigator/resource navigator)]
    (testing "Calling GET returns 200"
      (is (= 200 (navigator/status navigator))))
    (testing "properties"
      (is (= (:sku product) (hal/get-property resource :sku)))
      (is (= (:name product) (hal/get-property resource :name)))
      (is (= (:description product) (hal/get-property resource :description)))
      (is (= (:price product) (hal/get-property resource :price))))))

(deftest get-products-test
  (let [{:keys [address]} (extract @test-system)
        product1 {:sku 1
                  :name "pants"
                  :description "two legs"
                  :price 10}
        product2 {:sku 2
                  :name "shirt"
                  :description "two sleves"
                  :price 20}
        _ (-> (navigator/discover address)
              (navigator/post :products product1))
        _ (-> (navigator/discover address)
              (navigator/post :products product2))
        navigator (-> (navigator/discover address)
                      (navigator/get :products))
        resource (navigator/resource navigator)]
    (testing "returns 200"
      (is (= 200 (navigator/status navigator))))
    (testing "properties"
      (is (= 2 (count (hal/get-property resource :products)))))))
