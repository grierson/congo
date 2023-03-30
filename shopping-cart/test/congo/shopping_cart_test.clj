(ns congo.shopping-cart-test
  (:require [clojure.test :refer [deftest is testing]]
            [congo.shopping-cart :refer [app]]))

(deftest health-test
  (testing "Calling health returns 200"
    (is (= {:status 200
            :body  "healthy"}
           (app {:request-method :get
                 :uri "/health"})))))
