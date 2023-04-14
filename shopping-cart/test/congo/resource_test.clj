(ns congo.resource-test
  (:require [clojure.test :refer [deftest is testing]]
            [congo.resource :refer [app]]
            [congo.shopping-cart :as shopping-cart]
            [jsonista.core :as j]
            [congo.event-store :as events]
            [congo.product-catalog :as product-catalog]))

(defn setup []
  (let [shopping-cart (shopping-cart/make-store)
        events (events/make-store (str (random-uuid)))
        product-catalog (product-catalog/make-store)]
    (app {:shopping-cart-store shopping-cart
          :event-store events
          :product-catalog-gateway product-catalog})))

(deftest health-test
  (let [sut (setup)]
    (testing "Calling health returns 200"
      (is (= {:status 200
              :body  "healthy"}
             (sut {:request-method :get
                   :uri "/health"}))))))
(deftest events-test
  (let [sut (setup)
        _ (sut {:request-method :post
                :uri (str  "/shoppingcart/" 1 "/items")
                :body-params {:product-ids [1]}})
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
  (let [sut (setup)]
    (testing "GET returns cart"
      (let [request (sut {:request-method :get
                          :uri (str  "/shoppingcart/" 1)})]
        (testing "returns 200"
          (is (= 200 (:status request))))
        (testing "body"
          (is (= {:user-id 1
                  :items []}
                 (-> request
                     :body
                     slurp
                     (j/read-value j/keyword-keys-object-mapper)))))))))

(deftest POST-cart-test
  (let [sut (setup)]
    (testing "POST adds item to cart"
      (let [cart-id 1
            product-id 1
            request (sut {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})]
        (testing "returns 200"
          (is (= 200 (:status request))))))))

(deftest POST-multiple-cart-test
  (let [cart-id 1
        product-id-1 1
        product-id-2 2
        sut (setup)]
    (testing "POST adds item to cart"
      (let [request (sut {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id-1 product-id-2]}})]
        (testing "returns 200"
          (is (= 200 (:status request))))))))

(deftest DELETE-cart-test
  (let [cart-id 1
        product-id 1
        sut (setup)]
    (testing "DELETE removes item from cart"
      (let [request (sut {:request-method :delete
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})]
        (testing "returns 200"
          (is (= 200 (:status request))))))))
