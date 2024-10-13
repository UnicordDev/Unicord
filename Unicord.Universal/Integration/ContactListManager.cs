using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Enums;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Contacts;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Streams;
using Unicord.Universal.Services;
using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace Unicord.Universal.Integration
{
    internal class ContactListManager
    {
#if RELEASE
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
            var logger = Logger.GetLogger<ContactListManager>();
            var relationships = DiscordManager.Discord.Relationships.ToFrozenDictionary();

            try
            {
                var folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("AvatarCache", CreationCollisionOption.OpenIfExists);
                var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions

                if (store != null)
                {
                    var lists = await store.FindContactListsAsync();
                    var list = lists.FirstOrDefault(l => l.DisplayName == CONTACT_LIST_NAME) ?? (await store.CreateContactListAsync(CONTACT_LIST_NAME));

                    var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                    var annotationList = await GetAnnotationListAsync(annotationStore);
                    var allContacts = await store.FindContactsAsync();

                    // remove all contacts no longer in the user's friends list
                    var removed = allContacts.Where(c => c.RemoteId.StartsWith(REMOTE_ID_PREFIX) && !relationships.ContainsKey(ulong.TryParse(c.RemoteId.Split('_').Last(), out var id) ? id : 0));
                    foreach (var cont in removed)
                    {
                        try
                        {
                            await list.DeleteContactAsync(cont);
                        }
                        catch { }
                    }

                    // update all contacts
                    //var relationships = DiscordManager.Discord.Relationships.Values.Where(r => r.RelationshipType == DiscordRelationshipType.Friend);
                    foreach (var relationship in relationships.Values.Where(r => r.RelationshipType == DiscordRelationshipType.Friend))
                    {
                        await AddOrUpdateContactAsync(logger, relationship, list, annotationList, folder);
                    }

                    App.LocalSettings.Save<string>("ContactAvatarHashes", relationships.ToDictionary(k => k.Key.ToString(), v => v.Value.User.AvatarHash));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failde to sync contacts!");
            }
        }

        private static async Task<Contact> AddOrUpdateContactAsync(ILogger<ContactListManager> logger, DiscordRelationship relationship, ContactList list, ContactAnnotationList annotationList, StorageFolder folder)
        {
            Contact contact;
            if ((contact = await list.GetContactFromRemoteIdAsync(string.Format(REMOTE_ID_FORMAT, relationship.User.Id))) == null)
            {
                logger.LogInformation("Creating new contact for user {User}", relationship.User);
                contact = new Contact { RemoteId = string.Format(REMOTE_ID_FORMAT, relationship.User.Id) };
            }

            if (contact.Name != relationship.User.DisplayName)
            {
                logger.LogDebug("Updating contact username for {User} ({OldName} => {NewName})", relationship.User, contact.Name, relationship.User.DisplayName);
                contact.Name = relationship.User.DisplayName;
            }

            var currentHash = App.LocalSettings.Read<string>("ContactAvatarHashes", relationship.User.Id.ToString(), null);
            if ((currentHash == null && relationship.User.AvatarHash != null) || relationship.User.AvatarHash != currentHash)
            {
                logger.LogDebug("Updating contact avatar for {User}", relationship.User);
                contact.SourceDisplayPicture = await GetAvatarReferenceAsync(relationship.User, folder);
            }

            await list.SaveContactAsync(contact);

            var annotations = await annotationList.FindAnnotationsByRemoteIdAsync(contact.RemoteId);
            if (!annotations.Any())
            {
                logger.LogDebug("Creating new contact annotation for user {User}", relationship.User);

                var annotation = new ContactAnnotation()
                {
                    ContactId = contact.Id,
                    RemoteId = string.Format(REMOTE_ID_FORMAT, relationship.User.Id),
                    SupportedOperations = ContactAnnotationOperations.Share |
                        ContactAnnotationOperations.AudioCall | 
                        ContactAnnotationOperations.Message | 
                        ContactAnnotationOperations.ContactProfile | 
                        ContactAnnotationOperations.SocialFeeds |
                        ContactAnnotationOperations.VideoCall,
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
            if (string.IsNullOrWhiteSpace(user.AvatarHash))
            {
                return RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/example-avatar.png"));
            }

            var tempFile = await folder.TryGetItemAsync($"{user.AvatarHash}.jpg") as StorageFile;
            if (tempFile == null)
            {
                tempFile = await folder.CreateFileAsync($"{user.AvatarHash}.jpg", CreationCollisionOption.FailIfExists);

                var fileStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite);
                var stream = await Tools.HttpClient.GetInputStreamAsync(new Uri(user.GetAvatarUrl(ImageFormat.Jpeg, 256)));
                await RandomAccessStream.CopyAndCloseAsync(stream, fileStream);
            }

            return RandomAccessStreamReference.CreateFromFile(tempFile);
        }

        private static async Task<ContactAnnotationList> GetAnnotationListAsync(ContactAnnotationStore store)
        {
            var lists = await store.FindAnnotationListsAsync();
            var list = lists.FirstOrDefault();
            if (list == null)
            {
                list = await store.CreateAnnotationListAsync();
            }

            return list;
        }
    }
}
