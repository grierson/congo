(ns congo.resource-test
  (:require
   [clojure.test :refer [deftest is testing use-fixtures]]
   [congo.event-store :as events]
   [congo.system :as system]
   [donut.system :as ds]
   [jsonista.core :as j]
   [congo.shopping-cart :as shopping-cart]
   [congo.product-catalog :as product-catalog]))

(use-fixtures :each (ds/system-fixture ::system/test))

(deftest health-test
  (let [handler (get-in ds/*system* [::ds/instances :http :handler])
        request (handler {:request-method :get
                          :uri "/health"})]
    (testing "Calling health returns 200"
      (is (= {:status 200
              :body  "healthy"}
             request)))))

(deftest events-test
  (let [handler (get-in ds/*system* [::ds/instances :http :handler])
        event-store (get-in ds/*system* [::ds/instances :components :event-store])
        _ (handler {:request-method :post
                    :uri (str  "/shoppingcart/" 1 "/items")
                    :body-params {:product-ids [1]}})
        request (handler {:request-method :get
                          :uri "/events"})]
    (testing "/events"
      (testing "Calling events returns 200"
        (is (= 200 (:status request))))
      (testing "Contains events"
        (is (= 1
               (-> request
                   :body
                   slurp
                   (j/read-value j/keyword-keys-object-mapper)
                   count)))))
    (testing "Contains events"
      (let [events (events/get-events event-store)
            event (first events)]
        (is (= 1 (count events)))
        (is (= "ShoppingCartItemAdded" (:EVENTS/TYPE event)))))))

(deftest GET-cart-test
  (let [handler (get-in ds/*system* [::ds/instances :http :handler])
        shopping-cart-store (get-in ds/*system* [::ds/instances :components :shopping-cart-store])
        shopping-cart-id 1
        expected {:user-id 1
                  :items []}]
    (testing "GET returns cart"
      (let [request (handler {:request-method :get
                              :uri (str  "/shoppingcart/" shopping-cart-id)})]
        (testing "request"
          (testing "returns 200"
            (is (= 200 (:status request))))
          (testing "body"
            (is (= {:user-id 1
                    :items []}
                   (-> request
                       :body
                       slurp
                       (j/read-value j/keyword-keys-object-mapper))))))
        (testing "store"
          (is (= expected
                 (shopping-cart/get-cart shopping-cart-store shopping-cart-id))))))))

(deftest POST-cart-test
  (let [handler (get-in ds/*system* [::ds/instances :http :handler])
        shopping-cart-store (get-in ds/*system* [::ds/instances :components :shopping-cart-store])
        product-catalog-gateway (get-in ds/*system* [::ds/instances :components :product-catalog-gateway])
        cart-id 1
        product-id 1
        request (handler {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})
        product (first (product-catalog/get-products product-catalog-gateway [product-id]))
        expected {:user-id cart-id
                  :items [product]}]
    (testing "POST adds item to cart"
      (testing "returns 200"
        (is (= 200 (:status request))))
      (testing "returns body"
        (is (= expected
               (-> request
                   :body
                   slurp
                   (j/read-value j/keyword-keys-object-mapper))))))
    (testing "Shopping cart store"
      (is (= expected
             (shopping-cart/get-cart shopping-cart-store cart-id))))))

(deftest POST-multiple-cart-test
  (let [handler (get-in ds/*system* [::ds/instances :http :handler])
        shopping-cart-store (get-in ds/*system* [::ds/instances :components :shopping-cart-store])
        product-catalog-gateway (get-in ds/*system* [::ds/instances :components :product-catalog-gateway])
        cart-id 1
        product-id-1 1
        product-id-2 2
        [product-1 product-2] (product-catalog/get-products product-catalog-gateway [product-id-1 product-id-2])
        request (handler {:request-method :post
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id-1 product-id-2]}})
        expected {:user-id cart-id
                  :items [product-1 product-2]}]
    (testing "POST adds item to cart"
      (testing "returns 200"
        (is (= 200 (:status request))))
      (testing "body"
        (is (= expected
               (-> request
                   :body
                   slurp
                   (j/read-value j/keyword-keys-object-mapper))))))
    (testing "Shopping cart store"
      (is (= expected
             (shopping-cart/get-cart shopping-cart-store cart-id))))))

(deftest DELETE-cart-test
  (let [handler (get-in ds/*system* [::ds/instances :http :handler])
        cart-id 1
        product-id 1
        request (handler {:request-method :delete
                          :uri (str  "/shoppingcart/" cart-id "/items")
                          :body-params {:product-ids [product-id]}})]
    (testing "DELETE removes item from cart"
      (testing "returns 200"
        (is (= 200 (:status request))))
      (testing "returns body"
        (is (= {:user-id cart-id
                :items []}
               (-> request
                   :body
                   slurp
                   (j/read-value j/keyword-keys-object-mapper))))))))
