(ns template.product.route
  (:require
   [clojure.pprint :as pprint]
   [halboy.json :as haljson]
   [halboy.resource :as resource]
   [reitit.core :as reitit]
   [template.audit :as audit]
   [template.product.projection :as projection]
   [template.resources.urls :as urls]))

(defn get-handler [{:keys [database]} request]
  (let [{::reitit/keys [router]} request
        {{{:keys [id]} :path} :parameters} request
        product (projection/project database id)
        self-url (urls/url-for router request :product {:id id})]
    {:status 200
     :body (-> (resource/new-resource self-url)
               (resource/add-links
                {:discovery (urls/url-for router request :discovery)})
               (resource/add-properties product)
               (haljson/resource->json))}))

(require 'hashp.core)

(defn list-handler [{:keys [database]} request]
  (let [{::reitit/keys [router]} request
        skus  (get-in request [:parameters :query :skus])
        projections
        (if (nil? skus)
          (audit/get-projections database)
          (audit/get-projections-by-id database skus))
        self-url (urls/url-for router request :products)]
    {:status 200
     :body (-> (resource/new-resource self-url)
               (resource/add-links
                {:discovery (urls/url-for router request :discovery)})
               (resource/add-property :products (map :projections/data projections))
               (haljson/resource->json))}))

(defn create-handler [{:keys [database]} request]
  (let [{::reitit/keys [router]} request
        {{{:keys [sku name description price]} :body} :parameters} request
        {:keys [:projections/id :projections/data]}
        (projection/create-projection! database {:sku sku
                                                 :name name
                                                 :description description
                                                 :price price})
        self-url (urls/url-for router request :product {:id id})]
    {:status 201
     :headers {"Location" self-url}
     :body (-> (resource/new-resource self-url)
               (resource/add-links {:discovery (urls/url-for router request :discovery)})
               (resource/add-properties data)
               (haljson/resource->json))}))

(defn route [dependencies]
  [["/product/:id"
    {:name :product
     :get {:parameters {:path {:id uuid?}}
           :handler (partial get-handler dependencies)}}]
   ["/products"
    {:name :products
     :get {:parameters {:query [:map [:skus {:optional true} [:vector int?]]]}
           :handler (partial list-handler dependencies)}
     :post {:parameters {:body [:map
                                [:sku int?]
                                [:name string?]
                                [:description string?]
                                [:price int?]]}
            :handler (partial create-handler dependencies)}}]])
