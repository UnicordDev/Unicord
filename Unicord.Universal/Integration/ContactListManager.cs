using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Unicord.Universal.Integration
{
    internal static class ContactListManager
    {
#if STORE
        private const string REMOTE_ID_PREFIX = "Unicord_";
        private const string CONTACT_LIST_NAME = "Unicord";
        private const string APP_ID = "24101WamWooWamRD.Unicord_g9xp2jqbzr3wg!App";
#else
        private const string REMOTE_ID_PREFIX = "UnicordCanary_";
        private const string CONTACT_LIST_NAME = "Unicord Canary";
        private const string APP_ID = "24101WamWooWamRD.Unicord.Canary_g9xp2jqbzr3wg!App";
#endif

        public static async Task<ulong> TryGetChannelIdAsync(Contact contact)
        {
            var contacts = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite);
            var manager = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
            contact = await contacts.GetContactAsync(contact.Id);
            var annotations = await manager.FindAnnotationsForContactAsync(contact);
            var annotation = annotations.FirstOrDefault();

            if (ulong.TryParse(annotation.RemoteId.Split('_').Last(), out var id))
            {
                return id;
            }

            return 0;
        }

        public static async Task UpdateContactsListAsync()
        {
            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("AvatarCache", CreationCollisionOption.OpenIfExists);
                var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions
                if (store != null)
                {
                    var lists = await store.FindContactListsAsync();
                    var list = lists.FirstOrDefault() ?? (await store.CreateContactListAsync(CONTACT_LIST_NAME));

                    var allContacts = await store.FindContactsAsync();
                    var removed = allContacts.Where(c =>
                    {
                        if (ulong.TryParse(c.RemoteId.Split('_').Last(), out var id))
                        {
                            return !App.Discord.Relationships.ContainsKey(id);
                        }

                        return false;
                    });

                    foreach (var cont in removed)
                    {
                        await list.DeleteContactAsync(cont);
                    }

                    var contactsToAnnotate = new Dictionary<DiscordRelationship, Contact>();

                    foreach (var relationship in App.Discord.Relationships.Values.Where(r => r.RelationshipType == DiscordRelationshipType.Friend))
                    {
                        var contact = await AddOrUpdateContactForRelationship(list, relationship, folder);
                        if (contact != null)
                        {
                            contactsToAnnotate[relationship] = contact;
                        }
                    }

                    if (ApiInformation.IsTypePresent("Windows.ApplicationModel.Contacts.ContactAnnotation"))
                    {
                        var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                        var annotationList = await Tools.GetAnnotationlistAsync(annotationStore);
                        foreach (var contact in contactsToAnnotate)
                        {
                            var annotation = new ContactAnnotation()
                            {
                                ContactId = contact.Value.Id,
                                RemoteId = REMOTE_ID_PREFIX + contact.Key.User.Id.ToString(),
                                SupportedOperations = ContactAnnotationOperations.Share | ContactAnnotationOperations.AudioCall | ContactAnnotationOperations.Message | ContactAnnotationOperations.ContactProfile | ContactAnnotationOperations.SocialFeeds | ContactAnnotationOperations.VideoCall
                            };

                            annotation.ProviderProperties.Add("ContactPanelAppID", APP_ID);
                            annotation.ProviderProperties.Add("ContactShareAppID", APP_ID);

                            await annotationList.TrySaveAnnotationAsync(annotation);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to sync contacts!");
                Logger.Log(ex.ToString());
            }
        }

        public static async Task<Contact> AddOrUpdateContactForRelationship(ContactList list, DiscordRelationship relationship, StorageFolder folder = null)
        {
            Contact contact = null;

            if (folder == null)
            {
                folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("AvatarCache", CreationCollisionOption.OpenIfExists);
            }

            if ((contact = await list.GetContactFromRemoteIdAsync($"{REMOTE_ID_PREFIX}{relationship.User.Id}")) == null)
            {
                var reference = await GetAvatarReferenceAsync(relationship.User, folder);

                contact = new Contact
                {
                    DisplayNameOverride = relationship.User.Username,
                    SourceDisplayPicture = reference,
                    RemoteId = REMOTE_ID_PREFIX + relationship.User.Id.ToString()
                };

                contact.ProviderProperties["AvatarHash"] = relationship.User.AvatarHash;

                await list.SaveContactAsync(contact);
            }
            else
            {
                if (contact.ProviderProperties.TryGetValue("AvatarHash", out var obj) && obj is string str)
                {
                    if (relationship.User.AvatarHash != str)
                    {
                        var reference = await GetAvatarReferenceAsync(relationship.User, folder);
                        contact.SourceDisplayPicture = reference;
                        contact.ProviderProperties["AvatarHash"] = relationship.User.AvatarHash;

                        await list.SaveContactAsync(contact);
                    }
                }
            }

            return contact;
        }

        public static async Task ClearContactsAsync()
        {
            try
            {
                var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions
                if (store != null)
                {
                    var lists = await store.FindContactListsAsync();
                    var list = lists.FirstOrDefault() ?? (await store.CreateContactListAsync(CONTACT_LIST_NAME));
                    await list.DeleteAsync();
                }
            }
            catch { }
        }

        private static async Task<RandomAccessStreamReference> GetAvatarReferenceAsync(DiscordUser user, StorageFolder folder)
        {
            var tempFile = await folder.TryGetItemAsync($"{user.AvatarHash}.jpeg") as StorageFile;

            if (tempFile == null)
            {
                tempFile = await folder.CreateFileAsync($"{user.AvatarHash}.jpeg", CreationCollisionOption.FailIfExists);

                using (var fileStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                using (var stream = await Tools.HttpClient.GetInputStreamAsync(new Uri(user.GetAvatarUrl(ImageFormat.Jpeg, 256))))
                {
                    await RandomAccessStream.CopyAndCloseAsync(stream, fileStream);
                }
            }

            var reference = RandomAccessStreamReference.CreateFromFile(tempFile);
            return reference;
        }
    }
}
