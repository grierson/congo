(ns congo.shopping-cart-test
  (:require [clojure.test :refer [deftest is testing]]
            [freeport.core :refer [get-free-port!]]
            [congo.shopping-cart :refer [app]]
            [jsonista.core :as j]
            [taoensso.carmine :as car :refer [wcar]])
  (:import (com.github.fppt.jedismock RedisServer)))

(def server (RedisServer/newRedisServer))
(def redis-host (.getHost server))
(def redis-port (.getBindPort server))
(.start server)
(def redis-url (str "redis://" redis-host ":" redis-port))

(def my-wcar-opts
  {:pool (car/connection-pool {})
   :spec {:uri redis-url}})

(wcar my-wcar-opts (car/ping))
(wcar my-wcar-opts (car/set "foo" "bar"))
(wcar my-wcar-opts (car/get "foo"))

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
        (is (= {:user-id 42
                :items [{:name "t-shirt"}]}
               (-> request
                   :body
                   slurp
                   (j/read-value j/keyword-keys-object-mapper))))))))
