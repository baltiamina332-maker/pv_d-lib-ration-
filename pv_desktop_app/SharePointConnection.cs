using System;
using System.Configuration;
using System.Security;
using Microsoft.SharePoint.Client;

namespace DesktopApp
{
    /// <summary>
    /// Classe pour gérer la connexion et les opérations SharePoint
    /// </summary>
    public class SharePointConnection
    {
        private string siteUrl;
        private string username;
        private string password;
        private ClientContext clientContext;

        public SharePointConnection()
        {
            // Récupérer les paramètres SharePoint depuis App.config
            siteUrl = ConfigurationManager.AppSettings["SharePointSiteUrl"];
            username = ConfigurationManager.AppSettings["SharePointUsername"];
            password = ConfigurationManager.AppSettings["SharePointPassword"];
        }

        /// <summary>
        /// Établir la connexion à SharePoint
        /// </summary>
        public bool Connect()
        {
            try
            {
                clientContext = new ClientContext(siteUrl);

                // Créer un SecureString pour le mot de passe
                SecureString securePassword = new SecureString();
                foreach (char c in password)
                {
                    securePassword.AppendChar(c);
                }

                // Configurer les credentials (méthode obsolète mais fonctionnelle)
                #pragma warning disable CS0618
                clientContext.Credentials = new SharePointOnlineCredentials(username, securePassword);
                #pragma warning restore CS0618

                // Tester la connexion
                Web web = clientContext.Web;
                clientContext.Load(web, w => w.Title);
                clientContext.ExecuteQuery();

                Console.WriteLine($"Connexion SharePoint réussie au site: {web.Title}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur de connexion SharePoint: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Obtenir le contexte client SharePoint
        /// </summary>
        public ClientContext GetClientContext()
        {
            return clientContext;
        }

        /// <summary>
        /// Lire des éléments d'une liste SharePoint
        /// </summary>
        public void ReadListItems(string listName)
        {
            try
            {
                List list = clientContext.Web.Lists.GetByTitle(listName);
                CamlQuery query = CamlQuery.CreateAllItemsQuery();
                ListItemCollection items = list.GetItems(query);

                clientContext.Load(items);
                clientContext.ExecuteQuery();

                Console.WriteLine($"Nombre d'éléments dans '{listName}': {items.Count}");

                foreach (ListItem item in items)
                {
                    Console.WriteLine($"ID: {item.Id}, Title: {item["Title"]}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la lecture de la liste: {ex.Message}");
            }
        }

        /// <summary>
        /// Ajouter un élément à une liste SharePoint
        /// </summary>
        public bool AddListItem(string listName, string title, object additionalFields = null)
        {
            try
            {
                List list = clientContext.Web.Lists.GetByTitle(listName);
                ListItemCreationInformation itemCreateInfo = new ListItemCreationInformation();
                ListItem newItem = list.AddItem(itemCreateInfo);

                newItem["Title"] = title;

                // Ajouter des champs supplémentaires si fournis
                // Exemple d'utilisation avec un dictionnaire

                newItem.Update();
                clientContext.ExecuteQuery();

                Console.WriteLine($"Élément ajouté avec succès à la liste '{listName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'ajout d'un élément: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Upload un fichier vers une bibliothèque SharePoint
        /// </summary>
        public bool UploadFile(string libraryName, string fileName, byte[] fileContent)
        {
            try
            {
                List documentLibrary = clientContext.Web.Lists.GetByTitle(libraryName);
                FileCreationInformation newFile = new FileCreationInformation
                {
                    Content = fileContent,
                    Url = fileName,
                    Overwrite = true
                };

                Microsoft.SharePoint.Client.File uploadFile = documentLibrary.RootFolder.Files.Add(newFile);
                clientContext.Load(uploadFile);
                clientContext.ExecuteQuery();

                Console.WriteLine($"Fichier '{fileName}' uploadé avec succès dans '{libraryName}'");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'upload du fichier: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Fermer la connexion SharePoint
        /// </summary>
        public void Disconnect()
        {
            if (clientContext != null)
            {
                clientContext.Dispose();
                Console.WriteLine("Connexion SharePoint fermée.");
            }
        }
    }
}
