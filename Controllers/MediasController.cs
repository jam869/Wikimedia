using Controllers;
using DAL;
using Models;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;



[AccessControl.UserAccess(Access.View)]
public class MediasController : Controller
{

    private void InitSessionVariables()
    {
        // Session is a dictionary that hold keys values specific to a session
        // Each user of this web application have their own Session
        // A Session has a default time out of 20 minutes, after time out it is cleared

        if (Session["CurrentMediaId"] == null) Session["CurrentMediaId"] = 0;
        if (Session["CurrentMediaTitle"] == null) Session["CurrentMediaTitle"] = "";
        if (Session["Search"] == null) Session["Search"] = false;
        if (Session["SearchString"] == null) Session["SearchString"] = "";
        if (Session["SelectedCategory"] == null) Session["SelectedCategory"] = "";
        if (Session["Categories"] == null) Session["Categories"] = DB.Medias.MediasCategories();
        if (Session["SortByTitle"] == null) Session["SortByTitle"] = true;
        if (Session["SortAscending"] == null) Session["SortAscending"] = true;
        ValidateSelectedCategory();
    }

    private void ResetCurrentMediaInfo()
    {
        Session["CurrentMediaId"] = 0;
        Session["CurrentMediaTitle"] = "";
    }

    private void ValidateSelectedCategory()
    {
        if (Session["SelectedCategory"] != null)
        {
            var selectedCategory = (string)Session["SelectedCategory"];
            var Medias = DB.Medias.ToList().Where(c => c.Category == selectedCategory);
            if (Medias.Count() == 0)
                Session["SelectedCategory"] = "";
        }
    }

    public ActionResult GetMediasCategoriesList(bool forceRefresh = false)
    {
        try
        {
            InitSessionVariables();

            bool search = (bool)Session["Search"];

            if (search)
            {
                return PartialView();
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }
    // This action produce a partial view of Medias
    // It is meant to be called by an AJAX request (from client script)
    public ActionResult GetMedias(bool forceRefresh = false)
    {
        try
        {
            IEnumerable<Media> result = null;
            if (DB.Medias.HasChanged || forceRefresh)
            {
                InitSessionVariables();
                User currentUser = Models.User.ConnectedUser; // Récupère l'utilisateur

                // FILTRE ÉTAPE B.6 : Partagé OU (Connecté ET Propriétaire) OU Admin
                result = DB.Medias.ToList().Where(m =>
                    m.Shared == true ||
                    (currentUser != null && (m.OwnerId == currentUser.Id || currentUser.Access == Access.Admin))
                );

                bool search = (bool)Session["Search"];
                string searchString = (string)Session["SearchString"];

                if (search)
                {
                    result = result.Where(c => c.Title.ToLower().Contains(searchString)).OrderBy(c => c.Title);
                    string SelectedCategory = (string)Session["SelectedCategory"];
                    if (SelectedCategory != "")
                        result = result.Where(c => c.Category == SelectedCategory);
                }

                if ((bool)Session["SortAscending"])
                {
                    if ((bool)Session["SortByTitle"])
                        result = result.OrderBy(c => c.Title);
                    else
                        result = result.OrderBy(c => c.PublishDate);
                }
                else
                {
                    if ((bool)Session["SortByTitle"])
                        result = result.OrderByDescending(c => c.Title);
                    else
                        result = result.OrderByDescending(c => c.PublishDate);
                }
                return PartialView(result);
            }
            return null;
        }
        catch (System.Exception ex)
        {
            return Content("Erreur interne" + ex.Message, "text/html");
        }
    }


    public ActionResult List()
    {
        ResetCurrentMediaInfo();
        return View();
    }

    public ActionResult ToggleSearch()
    {
        if (Session["Search"] == null) Session["Search"] = false;
        Session["Search"] = !(bool)Session["Search"];
        return RedirectToAction("List");
    }
    public ActionResult SortByTitle()
    {
        Session["SortByTitle"] = true;
        return RedirectToAction("List");
    }
    public ActionResult ToggleSort()
    {
        Session["SortAscending"] = !(bool)Session["SortAscending"];
        return RedirectToAction("List");
    }
    public ActionResult SortByDate()
    {
        Session["SortByTitle"] = false;
        return RedirectToAction("List");
    }

    public ActionResult SetSearchString(string value)
    {
        Session["SearchString"] = value.ToLower();
        return RedirectToAction("List");
    }

    public ActionResult SetSearchCategory(string value)
    {
        Session["SelectedCategory"] = value;
        return RedirectToAction("List");
    }
    public ActionResult About()
    {
        return View();
    }


    public ActionResult Details(int id)
    {
        Session["CurrentMediaId"] = id;
        Media Media = DB.Medias.Get(id);
        if (Media != null)
        {
            Session["CurrentMediaTitle"] = Media.Title;
            return View(Media);
        }
        return RedirectToAction("List");
    }
    [AccessControl.UserAccess(Access.Write)]
    public ActionResult Create()
    {
        return View(new Media());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [AccessControl.UserAccess(Access.Write)] 
    public ActionResult Create(Media Media)
    {
        User currentUser = Models.User.ConnectedUser;
        if (currentUser != null)
        {
            Media.OwnerId = currentUser.Id; 
        }
        Media.PublishDate = DateTime.Now;
        DB.Medias.Add(Media);
        return RedirectToAction("List");
    }
    [AccessControl.UserAccess(Access.Write)]
    public ActionResult Edit()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media Media = DB.Medias.Get(id);
            User currentUser = Models.User.ConnectedUser;

            if (Media != null)
            {
                if (currentUser != null && (Media.OwnerId == currentUser.Id || currentUser.Access == Access.Admin))
                {
                    return View(Media);
                }
                else
                {
                    currentUser.Online = false;
                    DB.Logins.UpdateLogoutByUserId(currentUser.Id);

                    Models.User.ConnectedUser = null;
                    Session.Abandon();

                    return Redirect("/Accounts/Login?message=Accès illégal! Vous avez été déconnecté.&success=false");
                }
            }
        }
        return RedirectToAction("List");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AccessControl.UserAccess(Access.Write)]
    public ActionResult Edit(Media Media)
    {
        

        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;

        Media storedMedia = DB.Medias.Get(id);
        if (storedMedia != null)
        {
            Media.Id = id; 
            Media.PublishDate = storedMedia.PublishDate; 
            Media.OwnerId = storedMedia.OwnerId; 
            DB.Medias.Update(Media);
        }
        return RedirectToAction("Details/" + id);
    }
    [AccessControl.UserAccess(Access.Write)]
    public ActionResult Delete()
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        if (id != 0)
        {
            Media media = DB.Medias.Get(id);
            User currentUser = Models.User.ConnectedUser;

            if (media != null)
            {
                if (currentUser != null && (media.OwnerId == currentUser.Id || currentUser.Access == Access.Admin))
                {
                    DB.Medias.Delete(id);
                }
                else
                {
                    currentUser.Online = false;
                    DB.Logins.UpdateLogoutByUserId(currentUser.Id);

                    Models.User.ConnectedUser = null;
                    Session.Abandon();

                    return Redirect("/Accounts/Login?message=Accès illégal! Vous avez été déconnecté.&success=false");
                }
            }
        }
        return RedirectToAction("List");
    }

    // This action is meant to be called by an AJAX request
    // Return true if there is a name conflict
    // Look into validation.js for more details
    // and also into Views/Medias/MediaForm.cshtml
    public JsonResult CheckConflict(string YoutubeId)
    {
        int id = Session["CurrentMediaId"] != null ? (int)Session["CurrentMediaId"] : 0;
        // Response json value true if name is used in other Medias than the current Media
        return Json(DB.Medias.ToList().Where(c => c.YoutubeId == YoutubeId && c.Id != id).Any(),
                    JsonRequestBehavior.AllowGet /* must have for CORS verification by client browser */);
    }

    public ActionResult GetMediaDetails(int id, bool forceRefresh = false)
    {
        // Ne retourne du HTML que si la base de données a changé ou si c'est forcé
        if (DB.Medias.HasChanged || forceRefresh)
        {
            Media media = DB.Medias.Get(id);
            if (media != null)
            {
                return PartialView("_DetailsPartial", media);
            }
        }
        // Retourne null si aucun changement, le script JS ne fera rien
        return null;
    }
}
