(ns congo.resource-test
  (:require [clojure.test :refer [deftest is testing use-fixtures]]
            [congo.resource :refer [app]]
            [jsonista.core :as j]
            [taoensso.carmine :as car :refer [wcar]])
  (:import (com.github.fppt.jedismock RedisServer)))

(def server (RedisServer/newRedisServer))
(.start server)
(def redis-host (.getHost server))
(def redis-port (.getBindPort server))
(def redis-url (str "redis://" redis-host ":" redis-port))

(def shopping-cart-store
  {:pool (car/connection-pool {})
   :spec {:uri redis-url}})

(def cart-id 42)

(defn clear-tkeys! []
  (wcar shopping-cart-store (car/del cart-id)))

(defn test-fixture [f] (clear-tkeys!) (f) (clear-tkeys!))

(use-fixtures :each test-fixture)

(deftest health-test
  (let [sut (app nil nil)]
    (testing "Calling health returns 200"
      (is (= {:status 200
              :body  "healthy"}
             (sut {:request-method :get
                   :uri "/health"}))))))

(deftest GET-cart-test
  (let [sut (app shopping-cart-store nil)]
    (testing "GET returns cart"
      (let [request (sut {:request-method :get :uri (str  "/shoppingcart/" cart-id)})]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id cart-id
                  :items []}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))))))

(deftest POST-cart-test
  (let [sut (app shopping-cart-store {1 {:product-catalog-id 1
                                         :product-name "t-shirt"}})]
    (testing "GET returns cart"
      (let [request (sut {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [1]}})]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id cart-id
                  :items [{:product-catalog-id 1
                           :product-name "t-shirt"}]}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))))))
