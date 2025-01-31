using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WorkGroupPortal.Models;

namespace WorkGroupPortal.Controllers
{
    public class UsersController : Controller
    {
        private readonly LabDBContext _context;

        public UsersController(LabDBContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AvailableContacts()
        {
            // getting UserId from Session
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);

                // Finding user's Id
                var user = _context.Users.FirstOrDefault(c => c.Id == userId);

                if (user != null)
                {
                    // Get all users except the current user
                    var users = _context.Users
                        .Where(u => u.Id != user.Id)
                        .ToList();

                    

                    // Load all contact status
                    var contactStatuses = _context.Contacts
                        .Where(c => c.UserId == user.Id)
                        .ToDictionary(c => c.ContactId, c => c.Status);

                    

                    ViewBag.ContactStatuses = contactStatuses;
                    ViewBag.CurrentUser = user;

                    return View(users);
                }
            }
            TempData["ErrorMessage"] = "Login First";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendRequest(int contactId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            // Check if a contact request already exists
            var existingRequest = _context.Contacts
                .FirstOrDefault(c => c.UserId == userId && c.ContactId == contactId);

            if (existingRequest == null)
            {
                // Create a new contact request
                var contact = new Contact
                {
                    UserId = userId,
                    ContactId = contactId,
                    Status = "Pending"
                };

                _context.Contacts.Add(contact);
                _context.SaveChanges();
            }

            return RedirectToAction("AvailableContacts");
        }

        [HttpGet]
        public IActionResult SeeContactRequests()
        {
            // getting UserId from Session
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);

                // Finding user
                var user = _context.Users.FirstOrDefault(c => c.Id == userId);

                if (user != null)
                {
                    // Get all incoming contact requests for the current user
                    var contactRequests = _context.Contacts
                        .Include(c => c.User) // Include sender user details
                        .Where(c => c.ContactId == user.Id && c.Status == "Pending")
                        .ToList();

                    return View((contactRequests, user));
                }
            }
            TempData["ErrorMessage"] = "Login First";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptContactRequest(int contactId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            // Find the specific contact request
            var contactRequest = _context.Contacts
                .FirstOrDefault(c => c.ContactId == userId && c.UserId == contactId && c.Status == "Pending");

            if (contactRequest != null)
            {
                // Update the status to Accepted
                contactRequest.Status = "Accepted";

                var ContactForThisUserToo = new Contact
                {
                    UserId = userId,
                    ContactId = contactId,
                    Status = "Accepted"
                };

                _context.Contacts.Add(ContactForThisUserToo);
                _context.SaveChanges();
            }
            TempData["SuccessMessage"] = "Contact was added successfully!";
            return RedirectToAction("SeeContactRequests");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectContactRequest(int contactId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            // Find the specific contact request
            var contactRequest = _context.Contacts
                .FirstOrDefault(c => c.ContactId == userId && c.UserId == contactId && c.Status == "Pending");

            if (contactRequest != null)
            {
                // Delete the contact request
                _context.Contacts.Remove(contactRequest);
                _context.SaveChanges();
            }
            TempData["RejectMessage"] = "Contact request got rejected!";
            return RedirectToAction("SeeContactRequests");
        }


        [HttpGet]
        public IActionResult SeeContacts()
        {
            // getting UserId from Session
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);

                // Finding user
                var user = _context.Users.FirstOrDefault(c => c.Id == userId);

                if (user != null)
                {
                    // Get Accepted contacts for the current user
                    var acceptedContacts = _context.Contacts
                    .Include(c => c.ContactNavigation)
                    .Where(c => c.UserId == user.Id && c.Status == "Accepted")
                    .ToList();

                    return View((acceptedContacts, user));
                }
            }
            TempData["ErrorMessage"] = "Login First";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGroupAndInvite(string groupName, List<int> contactIds)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            if(contactIds.Count.Equals(0))
            {
                TempData["ErrorMessage"] = "You cannot create a group without others!";
                return RedirectToAction("SeeContacts");
            }

            var group = new Models.Group
            {
                Name = groupName, 
                CreatedById = userId,  
                CreatedAt = DateTime.Now
            };

            //Group Created
            _context.Groups.Add(group);
            _context.SaveChanges();

            
            foreach (var contactId in contactIds)
            {
                // Check if the contact is accepted before sending the invitation
                var contact = _context.Contacts
                    .FirstOrDefault(c => c.ContactId == userId && c.UserId == contactId && c.Status == "Accepted");

                if (contact != null)
                {
                    var groupInvitation = new GroupInvitation
                    {
                        SenderId = userId,
                        ReceiverId = contactId,
                        GroupId = group.Id,  // Link to the created group
                        Status = "Pending",  // Set the invitation status as "Pending"
                        CreatedAt = DateTime.Now,
                        RespondedAt = null   // No response for now
                    };

                    //Group Invitation Created
                    _context.GroupInvitations.Add(groupInvitation);
                }
            }

            _context.SaveChanges();

            TempData["SuccessMessage"] = "You invited your friends successfully!";
            return RedirectToAction("SeeContacts");

        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteContact(int contactId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            // Find the contacts for both
            var contact1 = _context.Contacts
                .FirstOrDefault(c => c.ContactId == userId && c.UserId == contactId && c.Status == "Accepted");

            var contact2 = _context.Contacts
                 .FirstOrDefault(c => c.ContactId == contactId && c.UserId == userId && c.Status == "Accepted");

            if (contact1 != null && contact2!= null )
            {
                // Delete the contacts
                _context.Contacts.Remove(contact1);
                _context.Contacts.Remove(contact2);
                _context.SaveChanges();
            }

            TempData["DeletedMessage"] = "You are not friends anymore!";
            return RedirectToAction("SeeContacts");
        }




        [HttpGet]
        public IActionResult ManageGroupInvitations()
        {
            // getting UserId from Session
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);

                // Finding user
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);

                if (user != null)
                {
                    // Get group invitations that are Pending
                    var groupInvitations = _context.GroupInvitations
                    .Include(gi => gi.Sender)
                    .Include(gi => gi.Group)
                    .Where(gi => gi.ReceiverId == user.Id && gi.Status == "Pending")
                    .ToList();

                    return View((groupInvitations, user));
                }
            }
            TempData["ErrorMessage"] = "Login First";
            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AcceptInvitation(int invitationId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            // Find the specific invitation
            var groupInvitation = _context.GroupInvitations
                .FirstOrDefault(gi => gi.Id == invitationId);

            
            if (groupInvitation != null)
            {
                groupInvitation.Status = "Accepted";
                groupInvitation.RespondedAt = DateTime.Now;
                _context.SaveChanges();
            }

            //Group has "Accepted" invitations so it redirects the user to see the group
            return RedirectToAction("SeeGroups");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeclineInvitation(int invitationId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            // Find the specific invitation
            var groupInvitation = _context.GroupInvitations
                .Include(gi => gi.Group)
                .FirstOrDefault(gi => gi.Id == invitationId);


            if (groupInvitation != null)
            {
                    var otherInvitationsForThisGroup = _context.GroupInvitations
                    .Where(gi => gi.Id != invitationId && gi.GroupId == groupInvitation.GroupId)
                    .FirstOrDefault();

                if (otherInvitationsForThisGroup != null)
                {
                    // Delete invitation
                    _context.GroupInvitations.Remove(groupInvitation);
                    
                }
                else
                {
                    var groupToDelete = groupInvitation.Group;

                    //delete the group too if this is the last invitation
                    _context.GroupInvitations.Remove(groupInvitation);
                    _context.Groups.Remove(groupToDelete);
                    
                }
                

            }

            TempData["RejectedMessage"] = "You don't want to be a part of this group!";
            _context.SaveChanges();
            return RedirectToAction("ManageGroupInvitations");
        }


        [HttpGet]
        public IActionResult SeeGroups(int? id)
        {
            // getting UserId from Session
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);

                // Finding user
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);

                if (user != null)
                {
                    if(!id.HasValue)
                    {
                        // Get groups that user has accepted
                        var groupsAccepted = _context.GroupInvitations
                            .Where(gi => (gi.SenderId == user.Id && gi.Status == "Accepted") || (gi.ReceiverId == user.Id && gi.Status == "Accepted"))
                            .Select(g => g.Group)
                            .Distinct()
                            .ToList();

                        return View((groupsAccepted, user));
                    }
                    else
                    {
                        // Get groups that user has accepted with one opened group
                        var groupsAccepted = _context.GroupInvitations
                            .Where(gi => (gi.SenderId == user.Id && gi.Status == "Accepted") || (gi.ReceiverId == user.Id && gi.Status == "Accepted"))
                            .Select(g => g.Group)
                            .Distinct()
                            .ToList();

                        ViewBag.AutoOpenGroupChatGroupId = id;
                        ViewBag.AutoOpenGroupChat = "x";
                        return View((groupsAccepted, user));


                    }
                    
                }
            }
            TempData["ErrorMessage"] = "Login First";
            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteGroup(int id)
        {
            var group = _context.Groups.Include(g => g.GroupInvitations).FirstOrDefault(g => g.Id == id);

            if (group != null)
            {
                //Group and invitations for this group deleted
                _context.GroupInvitations.RemoveRange(group.GroupInvitations);
                _context.Groups.Remove(group);
                _context.SaveChanges();
            }

            return RedirectToAction("SeeGroups");
        }

        /*[HttpPost]
        public IActionResult DeleteGroupMember(int groupId, int memberId)
        {
            var invitation = _context.GroupInvitations.FirstOrDefault(gi => gi.GroupId == groupId && gi.ReceiverId == memberId);

            if (invitation != null)
            {
                _context.GroupInvitations.Remove(invitation);
                _context.SaveChanges();
            }

            return RedirectToAction("SeeGroups");
        }*/

        [HttpPost]
        public IActionResult ExitFromGroup(int groupId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            var invitation = _context.GroupInvitations.FirstOrDefault(gi => gi.GroupId == groupId && gi.ReceiverId == userId);

            if (invitation != null)
            {
                //this invitation is deleted
                _context.GroupInvitations.Remove(invitation);
                _context.SaveChanges();

                var remainingInvitations = _context.GroupInvitations.Any(gi => gi.GroupId == groupId);

                if (!remainingInvitations)
                {
                    // If there are no invitations left delete the group too
                    var group = _context.Groups.FirstOrDefault(g => g.Id == groupId);
                    if (group != null)
                    {
                        _context.Groups.Remove(group);
                        _context.SaveChanges();
                    }
                }
                
            }

            return RedirectToAction("SeeGroups");
        }


        [HttpGet]
        public IActionResult SeeMessagesOfGroup(int Id)
        {
            // getting UserId from Session
            var userIdString = HttpContext.Session.GetString("UserId");

            if (!string.IsNullOrEmpty(userIdString))
            {
                int userId = int.Parse(userIdString);

                // Finding user
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);

                if (user != null)
                {
                    var group = _context.Groups
                        .Include(gi => gi.GroupInvitations)
                            .ThenInclude(gir => gir.Receiver)
                        .Include(m => m.Messages)
                            .ThenInclude(s => s.Sender)
                        .Include(g => g.CreatedBy)
                        .FirstOrDefault(g => g.Id == Id);

                    if (group != null)
                    {
                        var groupLeader = group.CreatedBy.Username;
                        
                        var acceptedMembers = group.GroupInvitations
                        .Where(gi => gi.Status == "Accepted")
                        .Select(gi => gi.Receiver)
                        .ToList();

                        var pendingMembers = group.GroupInvitations
                            .Where(gi => gi.Status == "Pending")
                            .Select(gi => gi.Receiver)
                            .ToList();

                        var messages = group.Messages
                            .OrderBy(m => m.SentAt)
                            .Select(m => new MessageViewModel
                            {
                                SenderId = m.SenderId,
                                SenderName = m.Sender.Username,
                                Content = m.Content,
                                SentAt = m.SentAt,
                                IsCurrentUser = m.SenderId == userId
                            })
                            .ToList();

                        var viewModel = new SeeMessagesOfGroupViewModel
                        {
                            GroupId = group.Id, // group id
                            GroupName = group.Name, // group name
                            GroupLeader = groupLeader, // createdby
                            AcceptedMembers = acceptedMembers, // group invited that accepted
                            PendingMembers = pendingMembers, // group invited that not accepted yet
                            Messages = messages // messages
                        };

                        return PartialView((viewModel, user));
                        /*return View((viewModel, user));*/
                    }



                }
                
            }
            TempData["ErrorMessage"] = "Login First";
            return RedirectToAction("Index", "Home");

        }

        [HttpPost]
        public IActionResult SendMessageToGroup(int groupId, string messageContent)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            var message = new Message
            {
                GroupId = groupId,
                SenderId = userId,
                Content = messageContent,
                SentAt = DateTime.UtcNow
            };

            // created message
            _context.Messages.Add(message);
            _context.SaveChanges();

            //redirests to see groups with the id of the group to show the messages of this group
            return RedirectToAction("SeeGroups", new { Id = groupId });
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
