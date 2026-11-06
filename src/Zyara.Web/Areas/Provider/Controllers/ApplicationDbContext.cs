namespace Zyara.Web.Areas.Provider.Controllers
{
    internal class ApplicationDbContext
    {
        public object TimeSlots { get; internal set; }

        internal void SaveChanges()
        {
            throw new NotImplementedException();
        }
    }
}