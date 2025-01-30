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

                    

                    // Load all contact statuses for the current user
                    var contactStatuses = _context.Contacts
                        .Where(c => c.UserId == user.Id)
                        .ToDictionary(c => c.ContactId, c => c.Status);

                    

                    ViewBag.ContactStatuses = contactStatuses;
                    ViewBag.CurrentUser = user;

                    return View(users);
                }
            }
            TempData["ErrorMessage"] = "You must log in first.";
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
            TempData["ErrorMessage"] = "You must log in first.";
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
                    // Get accepted contacts for the current user
                    var acceptedContacts = _context.Contacts
                    .Include(c => c.ContactNavigation)
                    .Where(c => c.UserId == user.Id && c.Status == "Accepted")
                    .ToList();

                    return View((acceptedContacts, user));
                }
            }
            TempData["ErrorMessage"] = "You must log in first.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateGroupAndInvite(string groupName, List<int> contactIds)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            var group = new Models.Group
            {
                Name = groupName, // Group name from the form
                CreatedById = userId,  // User creating the group
                CreatedAt = DateTime.Now
                // Set other necessary group properties
            };

            // Add the new group to the context
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
                        GroupId = group.Id,  // Link to the newly created group
                        Status = "Pending",  // Set the invitation status as "Pending"
                        CreatedAt = DateTime.Now,
                        RespondedAt = null   // Initially, no response
                    };

                    _context.GroupInvitations.Add(groupInvitation);
                }
            }

            _context.SaveChanges();

            return RedirectToAction("SeeGroups");
            
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteContact(int contactId)
        {
            var userIdString = HttpContext.Session.GetString("UserId");
            int userId = int.Parse(userIdString);

            // Find the specific contacts
            var contact1 = _context.Contacts
                .FirstOrDefault(c => c.ContactId == userId && c.UserId == contactId && c.Status == "Accepted");

            var contact2 = _context.Contacts
                 .FirstOrDefault(c => c.ContactId == contactId && c.UserId == userId && c.Status == "Accepted");

            if (contact1 != null && contact2!= null )
            {
                // Delete the contact request
                _context.Contacts.Remove(contact1);
                _context.Contacts.Remove(contact2);
                _context.SaveChanges();
            }

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
            TempData["ErrorMessage"] = "You must log in first.";
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

            return RedirectToAction("ManageGroupInvitations");
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
                    // Delete this invitation
                    _context.GroupInvitations.Remove(groupInvitation);
                    
                }
                else
                {
                    var groupToDelete = groupInvitation.Group;

                    
                    _context.GroupInvitations.Remove(groupInvitation);
                    _context.Groups.Remove(groupToDelete);
                    
                }
                

            }

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
                        // Get groups that user has accepted
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
            TempData["ErrorMessage"] = "You must log in first.";
            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public IActionResult DeleteGroup(int groupId)
        {
            var group = _context.Groups.Include(g => g.GroupInvitations).FirstOrDefault(g => g.Id == groupId);

            if (group != null)
            {
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
                _context.GroupInvitations.Remove(invitation);
                _context.SaveChanges();

                var remainingInvitations = _context.GroupInvitations.Any(gi => gi.GroupId == groupId);

                if (!remainingInvitations)
                {
                    // If no invitations are left, delete the group
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
                            GroupId = group.Id,
                            GroupName = group.Name,
                            GroupLeader = groupLeader,
                            AcceptedMembers = acceptedMembers,
                            PendingMembers = pendingMembers /*?? new List<User>()*/,
                            Messages = messages /*?? new List<MessageViewModel>()*/
                        };

                        return PartialView((viewModel, user));
                        /*return View((viewModel, user));*/
                    }



                }
                
            }
            TempData["ErrorMessage"] = "You must log in first.";
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

            _context.Messages.Add(message);
            _context.SaveChanges();

            return RedirectToAction("SeeGroups", new { Id = groupId });
        }



        // GET: Users
        public async Task<IActionResult> Index()
        {
            return View(await _context.Users.ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Username,Email,Password,CreatedAt")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Username,Email,Password,CreatedAt")] User user)
        {
            if (id != user.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(m => m.Id == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
            {
                _context.Users.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
