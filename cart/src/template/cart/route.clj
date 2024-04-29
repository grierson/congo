(ns template.cart.route
  (:require
   [halboy.json :as haljson]
   [halboy.resource :as resource]
   [reitit.core :as reitit]
   [template.cart.projection :as projection]
   [template.audit :as audit]
   [template.resources.urls :as urls]))

(defn get-handler [{:keys [database]} request]
  (let [{::reitit/keys [router]} request
        {{{:keys [id]} :path} :parameters} request
        cart (projection/project database id)
        self-url (urls/url-for router request :cart {:id id})]
    {:status 200
     :body (-> (resource/new-resource self-url)
               (resource/add-links
                {:discovery (urls/url-for router request :discovery)})
               (resource/add-properties cart)
               (haljson/resource->json))}))

(defn list-handler [{:keys [database]} request]
  (let [{::reitit/keys [router]} request
        projections (audit/get-projections database)
        self-url (urls/url-for router request :carts)]
    {:status 200
     :body (-> (resource/new-resource self-url)
               (resource/add-links
                {:discovery (urls/url-for router request :discovery)})
               (resource/add-property :carts (map :projections/data projections))
               (haljson/resource->json))}))

(defn create-handler [{:keys [database]} request]
  (let [{::reitit/keys [router]} request
        {{{:keys [name]} :body} :parameters} request
        {:keys [:projections/id :projections/data]} (projection/create-projection! database {:name name})
        self-url (urls/url-for router request :cart {:id id})]
    {:status 201
     :headers {"Location" self-url}
     :body (-> (resource/new-resource self-url)
               (resource/add-links {:discovery (urls/url-for router request :discovery)})
               (resource/add-properties data)
               (haljson/resource->json))}))

(defn route [dependencies]
  [["/cart/:id"
    {:name :cart
     :get {:parameters {:path {:id uuid?}}
           :handler (partial get-handler dependencies)}}]
   ["/carts"
    {:name :carts
     :get {:handler (partial list-handler dependencies)}
     :post {:parameters {:body [:map [:name string?]]}
            :handler (partial create-handler dependencies)}}]])
