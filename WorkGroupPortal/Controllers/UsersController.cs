﻿using System;
using System.Collections.Generic;
using System.Linq;
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
                        .Where(c => c.UserId == user.Id && c.Status != "Accepted")
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

            var group = new Group
            {
                Name = groupName, // Group name from the form
                CreatedById = userId,  // User creating the group
                CreatedAt = DateTime.Now
                // Set other necessary group properties
            };

            // Add the new group to the context
            _context.Groups.Add(group);
            _context.SaveChanges();

            // Step 2: Send Invitations to the Contacts
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

            // Step 3: Save all the changes (group and invitations)
            _context.SaveChanges();

            return RedirectToAction("Index", "Home");
            /*return RedirectToAction("SeeGroups"); */
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
