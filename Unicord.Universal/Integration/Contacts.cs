using DSharpPlus;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Contacts;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Unicord.Universal.Integration
{
    internal static class Contacts
    {
        public static async Task UpdateContactsListAsync()
        {
            try
            {
                var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions
                var annotationStore = await ContactManager.RequestAnnotationStoreAsync(ContactAnnotationStoreAccessType.AppAnnotationsReadWrite);
                if (store != null)
                {
                    var lists = await store.FindContactListsAsync();
                    var list = lists.FirstOrDefault() ?? (await store.CreateContactListAsync("Unicord"));
                    var annotationList = await Tools.GetAnnotationlistAsync(annotationStore);

                    var allContacts = await store.FindContactsAsync();
                    var removed = allContacts.Where(c =>
                    {
                        if (ulong.TryParse(c.RemoteId.Split('_').Last(), out var id))
                        {
                            return !App.Discord.Relationships.Any(r => r.Id == id);
                        }

                        return false;
                    });

                    foreach (var cont in removed)
                    {
                        await list.DeleteContactAsync(cont);
                    }

                    foreach (var relationship in App.Discord.Relationships.Where(r => r.RelationshipType == DiscordRelationshipType.Friend))
                    {
                        await AddOrUpdateContactForRelationship(list, annotationList, relationship);
                    }
                }
            }
            catch { }
        }

        public static async Task AddOrUpdateContactForRelationship(ContactList list, ContactAnnotationList annotationList, DiscordRelationship relationship)
        {
            Contact contact = null;

            if ((contact = await list.GetContactFromRemoteIdAsync($"Unicord_{relationship.User.Id}")) == null)
            {
                var reference = await GetAvatarReferenceAsync(relationship);

                contact = new Contact
                {
                    DisplayNameOverride = relationship.User.Username,
                    SourceDisplayPicture = reference,
                    RemoteId = "Unicord_" + relationship.User.Id.ToString()
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
                        var reference = await GetAvatarReferenceAsync(relationship);
                        contact.SourceDisplayPicture = reference;
                        contact.ProviderProperties["AvatarHash"] = relationship.User.AvatarHash;

                        await list.SaveContactAsync(contact);
                    }
                }
            }

            var annotation = new ContactAnnotation()
            {
                ContactId = contact.Id,
                RemoteId = "Unicord_" + relationship.User.Id.ToString(),
                SupportedOperations = ContactAnnotationOperations.Share | ContactAnnotationOperations.AudioCall | ContactAnnotationOperations.Message | ContactAnnotationOperations.ContactProfile | ContactAnnotationOperations.SocialFeeds | ContactAnnotationOperations.VideoCall
            };

            annotation.ProviderProperties.Add("ContactPanelAppID", "24101WamWooWamRD.Unicord_g9xp2jqbzr3wg!App");

            await annotationList.TrySaveAnnotationAsync(annotation);
        }

        public static async Task ClearContactsAsync()
        {
            try
            {
                var store = await ContactManager.RequestStoreAsync(ContactStoreAccessType.AppContactsReadWrite); // requests contact permissions
                if (store != null)
                {
                    var lists = await store.FindContactListsAsync();
                    var list = lists.FirstOrDefault() ?? (await store.CreateContactListAsync("Unicord"));
                    await list.DeleteAsync();
                }
            }
            catch { }
        }

        private static async Task<RandomAccessStreamReference> GetAvatarReferenceAsync(DSharpPlus.Entities.DiscordRelationship relationship)
        {
            StorageFile tempFile = null;

            try
            {
                tempFile = await ApplicationData.Current.LocalFolder.CreateFileAsync($"{relationship.User.AvatarHash}.png", CreationCollisionOption.FailIfExists);

                using (var stream = await Tools.HttpClient.GetInputStreamAsync(new Uri(relationship.User.NonAnimatedAvatarUrl)))
                using (var fileStream = await tempFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    await stream.AsStreamForRead().CopyToAsync(fileStream.AsStreamForWrite());
                }
            }
            catch
            {
                tempFile = await ApplicationData.Current.LocalFolder.GetFileAsync($"{relationship.User.AvatarHash}.png");
            }

            var reference = RandomAccessStreamReference.CreateFromFile(tempFile);
            return reference;
        }
    }
}
