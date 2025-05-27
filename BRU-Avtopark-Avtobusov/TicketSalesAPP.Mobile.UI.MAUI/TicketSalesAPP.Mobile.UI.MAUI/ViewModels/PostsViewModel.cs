using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Data;
using TicketSalesAPP.Mobile.UI.MAUI.Domain.Services;

namespace TicketSalesAPP.Mobile.UI.MAUI.ViewModels
{
    public partial class PostsViewModel : ObservableObject
    {

        [ObservableProperty]
        bool canDeletePosts;

        [ObservableProperty]
        ObservableCollection<Post>? posts;

        [ObservableProperty]
        Author? currentUser;

        readonly ISecuredWebApiService dataStore;
        public PostsViewModel(ISecuredWebApiService dataStore)
        {
            this.dataStore = dataStore;
        }

        [RelayCommand]
        async Task InitializeAsync()
        {
            try
            {
                CanDeletePosts = await dataStore.UserCanDeletePostAsync();
                CurrentUser = await dataStore.CurrentUser();

                var data = await dataStore.GetItemsAsync();
                Posts = new ObservableCollection<Post>(data ?? Enumerable.Empty<Post>());
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }
        [RelayCommand]
        async Task DeletePost(Post post)
        {
            bool isDeleted = await dataStore.DeletePostAsync(post.PostId);
            if (!isDeleted)
            {
                await Shell.Current.DisplayAlert("Error", "Couldn't delete the post", "Ok");
            }
            else
            {
                Posts?.Remove(post);
            }
        }

        [RelayCommand]
        Task<Page> Logout()
        {
            return Shell.Current.Navigation.PopAsync();
        }
    }
}