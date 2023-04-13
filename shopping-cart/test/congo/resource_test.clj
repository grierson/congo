(ns congo.resource-test
  (:require [clojure.test :refer [deftest is testing use-fixtures]]
            [congo.resource :refer [app]]
            [congo.shopping-cart :as shopping-cart]
            [jsonista.core :as j]
            [congo.event-store :as events]
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

(def event-store (atom []))

(defn test-fixture [f]
  (clear-tkeys!)
  (reset! event-store [])
  (reset! events/counter 0)
  (f))

(use-fixtures :each test-fixture)

(deftest health-test
  (let [sut (app {})]
    (testing "Calling health returns 200"
      (is (= {:status 200
              :body  "healthy"}
             (sut {:request-method :get
                   :uri "/health"}))))))
(deftest events-test
  (let [product-id 1
        product-name "t-shirt"
        t-shirt {:product-catalog-id product-id
                 :product-name product-name}
        product-catalog-gateway {product-id t-shirt}
        sut (app {:shopping-cart-store shopping-cart-store
                  :product-catalog-gateway product-catalog-gateway
                  :event-store event-store})
        _ (sut {:request-method :post
                :uri (str  "/shoppingcart/" cart-id "/items")
                :body-params {:product-ids [product-id]}})
        request (sut {:request-method :get
                      :uri "/events"})]
    (testing "Calling events returns 200"
      (is (= 200 (:status request))))
    (testing "Contains events"
      (is (= 1
             (-> request
                 :body
                 slurp
                 (j/read-value j/keyword-keys-object-mapper)
                 count))))))

(deftest GET-cart-test
  (let [sut (app {:shopping-cart-store shopping-cart-store})]
    (testing "GET returns cart"
      (let [request (sut {:request-method :get
                          :uri (str  "/shoppingcart/" cart-id)})]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id cart-id
                  :items []}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))
        (testing "shopping cart store contains cart"
          (is (= {:user-id cart-id
                  :items []}
                 (shopping-cart/get-cart shopping-cart-store cart-id))))))))

(deftest POST-cart-test
  (let [product-id 1
        product-name "t-shirt"
        t-shirt {:product-catalog-id product-id
                 :product-name product-name}
        product-catalog-gateway {product-id t-shirt}
        sut (app {:shopping-cart-store  shopping-cart-store
                  :product-catalog-gateway product-catalog-gateway
                  :event-store event-store})]
    (testing "POST adds item to cart"
      (let [request (sut {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id cart-id
                  :items [{:product-catalog-id product-id
                           :product-name product-name}]}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))
        (testing "shopping cart contains new item"
          (is (= t-shirt
                 (-> (shopping-cart/get-cart shopping-cart-store cart-id)
                     :items
                     first))))
        (testing "event store contains items"
          (is (= 1 (count @event-store))))))))

(deftest POST-multiple-cart-test
  (let [product-id-1 1
        product-name-1 "t-shirt"
        product-id-2 2
        product-name-2 "pants"
        t-shirt {:product-catalog-id product-id-1
                 :product-name product-name-1}
        pants {:product-catalog-id product-id-2
               :product-name product-name-2}
        product-catalog-gateway {product-id-1 t-shirt
                                 product-id-2 pants}
        sut (app {:shopping-cart-store  shopping-cart-store
                  :product-catalog-gateway product-catalog-gateway
                  :event-store event-store})]
    (testing "POST adds item to cart"
      (let [request (sut {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id-1 product-id-2]}})]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id cart-id
                  :items [{:product-catalog-id product-id-1
                           :product-name product-name-1}
                          {:product-catalog-id product-id-2
                           :product-name product-name-2}]}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))
        (testing "shopping cart contains new item"
          (is (= t-shirt
                 (-> (shopping-cart/get-cart shopping-cart-store cart-id)
                     :items
                     first)))
          (is (= pants
                 (-> (shopping-cart/get-cart shopping-cart-store cart-id)
                     :items
                     second))))
        (testing "event store contains items"
          (is (= 2 (count @event-store))))))))

(deftest DELETE-cart-test
  (let [product-id 1
        t-shirt {:product-catalog-id product-id
                 :product-name "t-shirt"}
        cart (shopping-cart/get-cart shopping-cart-store cart-id)
        cart (shopping-cart/add-items event-store cart [t-shirt])
        _ (shopping-cart/save-cart shopping-cart-store cart)
        sut (app {:shopping-cart-store shopping-cart-store
                  :event-store event-store})]
    (testing "DELETE removes item from cart"
      (let [request (sut {:request-method :delete
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})]
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
                     :items))))
        (testing "event store contains add and delete events"
          (is (= 2
                 (count @event-store))))))))
