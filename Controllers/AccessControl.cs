using Models;
using System;
using System.Web;
using System.Web.Mvc;

namespace Controllers
{
    public class AccessControl
    {
        public class UserAccess : AuthorizeAttribute
        {
            private Access RequiredAccess { get; set; }

            public UserAccess(Access Access = Access.Anonymous) : base()
            {
                RequiredAccess = Access;
            }

            protected override bool AuthorizeCore(HttpContextBase httpContext)
            {
                try
                {
                    if (User.ConnectedUser == null)
                        return false;

                    if (User.ConnectedUser.Access < RequiredAccess || User.ConnectedUser.Blocked)
                        return false;

                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
            {
                if (User.ConnectedUser != null)
                {
                    User.ConnectedUser.Online = false;

                    DAL.DB.Logins.UpdateLogoutByUserId(User.ConnectedUser.Id);

                    User.ConnectedUser = null;
                    filterContext.HttpContext.Session.Abandon();
                }

                bool ajaxRequest = filterContext.HttpContext.Request.Headers["cors"] != null;
                if (!ajaxRequest)
                {
                    filterContext.Result = new RedirectResult("/Accounts/Login?message=Accès non autorisé! Vous avez été déconnecté par sécurité.&success=false");
                }
                else
                {
                    filterContext.Result = new HttpUnauthorizedResult();
                }
            }
        }
    }
}