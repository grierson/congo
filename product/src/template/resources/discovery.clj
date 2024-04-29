(ns template.resources.discovery
  (:require
   [halboy.json :as haljson]
   [halboy.resource :as resource]
   [reitit.core :as reitit]
   [template.resources.urls :as urls]))

(defn get-handler [request]
  (let [{::reitit/keys [router]} request
        self-url (urls/url-for router request :discovery)]
    {:status 200
     :body (-> (resource/new-resource self-url)
               (resource/add-links
                {:health (urls/url-for router request :health)
                 :event (str (urls/url-for router request :event) {:id "{id}"})
                 :events (str (urls/url-for router request :events) "{?start,end}")
                 :product (urls/url-for router request :product {:id "{id}"})
                 :products (urls/url-for router request :products)})
               (haljson/resource->json))}))

(def route ["/" {:name :discovery
                 :get {:handler get-handler}}])
