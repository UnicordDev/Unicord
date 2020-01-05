using System;
using System.Collections.Generic;
using System.Linq;
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
        private const string REMOTE_ID_FORMAT = "Unicord_{0}";
        private const string CONTACT_LIST_NAME = "Unicord";
        private const string APP_ID = "24101WamWooWamRD.Unicord_g9xp2jqbzr3wg!App";
#else
        private const string REMOTE_ID_PREFIX = "UnicordCanary_";
        private const string REMOTE_ID_FORMAT = "UnicordCanary_{0}";
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
                    var list = lists.FirstOrDefault(l => l.DisplayName == CONTACT_LIST_NAME) ?? (await store.CreateContactListAsync(CONTACT_LIST_NAME));

                    var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                    var annotationList = await Tools.GetAnnotationListAsync(annotationStore);
                    var allContacts = await store.FindContactsAsync();

                    // remove all contacts no longer in the user's friends list
                    var removed = allContacts.Where(c => c.RemoteId.StartsWith(REMOTE_ID_PREFIX) && !App.Discord.Relationships.ContainsKey(ulong.TryParse(c.RemoteId.Split('_').Last(), out var id) ? id : 0));
                    foreach (var cont in removed)
                    {
                        try
                        {
                            await list.DeleteContactAsync(cont);
                        }
                        catch { }
                    }

                    // update all contacts
                    var relationships = App.Discord.Relationships.Values.Where(r => r.RelationshipType == DiscordRelationshipType.Friend);
                    foreach (var relationship in relationships)
                    {
                        await AddOrUpdateContactAsync(relationship, list, annotationList, folder);
                    }

                    App.LocalSettings.Save<string>("ContactAvatarHashes", relationships.ToDictionary(k => k.User.Id.ToString(), v => v.User.AvatarHash));
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to sync contacts!");
                Logger.Log(ex.ToString());
            }
        }

        public static async Task<Contact> AddOrUpdateContactAsync(DiscordRelationship relationship, ContactList list = null, ContactAnnotationList annotationList = null, StorageFolder folder = null)
        {
            if (list == null)
            {
                var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions
                var lists = await store.FindContactListsAsync();
                list = lists.FirstOrDefault(l => l.DisplayName == CONTACT_LIST_NAME) ?? (await store.CreateContactListAsync(CONTACT_LIST_NAME));
            }

            if (annotationList == null)
            {
                var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                annotationList = await Tools.GetAnnotationListAsync(annotationStore);
            }

            folder ??= await ApplicationData.Current.LocalFolder.CreateFolderAsync("AvatarCache", CreationCollisionOption.OpenIfExists);

            Contact contact;
            if ((contact = await list.GetContactFromRemoteIdAsync(string.Format(REMOTE_ID_FORMAT, relationship.User.Id))) == null)
            {
                Logger.Log($"Creating new contact for user {relationship.User}");
                contact = new Contact { RemoteId = string.Format(REMOTE_ID_FORMAT, relationship.User.Id) };
            }

            if (contact.Name != relationship.User.Username)
            {
                Logger.Log($"Updating contact username for {relationship.User}");
                contact.Name = relationship.User.Username;
            }

            var currentHash = App.LocalSettings.Read<string>("ContactAvatarHashes", relationship.User.Id.ToString(), null);
            if (currentHash == null || relationship.User.AvatarHash != currentHash)
            {
                Logger.Log($"Updating contact avatar for {relationship.User}");
                contact.SourceDisplayPicture = await GetAvatarReferenceAsync(relationship.User, folder);
            }

            await list.SaveContactAsync(contact);

            var annotations = await annotationList.FindAnnotationsByRemoteIdAsync(contact.RemoteId);
            if (!annotations.Any())
            {
                Logger.Log($"Creating new contact annotation for user {relationship.User}");

                var annotation = new ContactAnnotation()
                {
                    ContactId = contact.Id,
                    RemoteId = string.Format(REMOTE_ID_FORMAT, relationship.User.Id),
                    SupportedOperations = ContactAnnotationOperations.Share | ContactAnnotationOperations.AudioCall | ContactAnnotationOperations.Message | ContactAnnotationOperations.ContactProfile | ContactAnnotationOperations.SocialFeeds | ContactAnnotationOperations.VideoCall,
                    ProviderProperties = { ["ContactPanelAppID"] = APP_ID, ["ContactShareAppID"] = APP_ID }
                };

                await annotationList.TrySaveAnnotationAsync(annotation);
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
                    var list = lists.FirstOrDefault(c => c.DisplayName == CONTACT_LIST_NAME) ?? (await store.CreateContactListAsync(CONTACT_LIST_NAME));
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

            return RandomAccessStreamReference.CreateFromFile(tempFile);
        }
    }
}
