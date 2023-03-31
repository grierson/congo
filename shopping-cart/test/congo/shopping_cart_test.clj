(ns congo.shopping-cart-test
  (:require [clojure.test :refer [deftest is testing]]
            [congo.shopping-cart :refer [app]]
            [jsonista.core :as j]))

(deftest health-test
  (testing "Calling health returns 200"
    (is (= {:status 200
            :body  "healthy"}
           (app {:request-method :get
                 :uri "/health"})))))

(deftest get-cart-test
  (testing "GET returns cart"
    (let [request (app {:request-method :get :uri "/shoppingcart/42"})]
      (testing "returns 200"
        (is (= 200 (:status request))))
      (testing "body"
        (is (= {:user-id 42}
               (-> request
                   :body
                   slurp
                   (j/read-value j/keyword-keys-object-mapper))))))))
