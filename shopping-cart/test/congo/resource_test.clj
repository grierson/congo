(ns congo.resource-test
  (:require [clojure.test :refer [deftest is testing use-fixtures]]
            [congo.resource :refer [app]]
            [congo.shopping-cart :as shopping-cart]
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
                     (j/read-value j/keyword-keys-object-mapper)))))
        (testing "shopping cart contains new item"
          (is (= {:user-id cart-id
                  :items []}
                 (shopping-cart/get-cart shopping-cart-store cart-id))))))))

(deftest POST-cart-test
  (let [product-id 1
        t-shirt {:product-catalog-id product-id
                 :product-name "t-shirt"}
        sut (app shopping-cart-store {product-id t-shirt})]
    (testing "POST adds item to cart"
      (let [request (sut {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id cart-id
                  :items [{:product-catalog-id product-id
                           :product-name "t-shirt"}]}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))
        (testing "shopping cart contains new item"
          (is (= t-shirt
                 (-> (shopping-cart/get-cart shopping-cart-store cart-id)
                     :items
                     first))))))))

(deftest DELETE-cart-test
  (let [product-id 1
        t-shirt {:product-catalog-id product-id
                 :product-name "t-shirt"}
        cart (shopping-cart/get-cart shopping-cart-store cart-id)
        cart (shopping-cart/add-items cart [t-shirt])
        _ (shopping-cart/save-cart shopping-cart-store cart)
        sut (app shopping-cart-store nil)]
    (testing "DELETE removes item from cart"
      (let [request (sut {:request-method :delete
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})
            _ (prn request)]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id cart-id
                  :items []}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))
        (testing "shopping cart contains no items"
          (is (= []
                 (-> (shopping-cart/get-cart shopping-cart-store cart-id)
                     :items))))))))
