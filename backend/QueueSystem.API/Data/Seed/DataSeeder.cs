using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Models;

namespace QueueSystem.API.Data.Seed;

public static class DataSeeder
{
    private const string OperatorPassword = "operator123";
    private const string AdminPassword = "admin123";
    private const string ManagerPassword = "manager123";

    public static async Task SeedAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Department> departments = await EnsureDepartmentsAsync(dbContext, cancellationToken);
        List<ServiceType> serviceTypes = await EnsureServiceTypesAsync(dbContext, cancellationToken);
        List<Counter> counters = await EnsureCountersAsync(dbContext, departments, cancellationToken);
        await EnsureCounterServiceTypesAsync(dbContext, counters, serviceTypes, cancellationToken);
        List<Operator> operators = await EnsureOperatorsAsync(dbContext, counters, cancellationToken);
        await EnsureUsersAsync(dbContext, operators, departments, cancellationToken);
        await EnsureOperationalDemoTicketsAsync(dbContext, counters, operators, serviceTypes, cancellationToken);
    }

    private static async Task<List<Department>> EnsureDepartmentsAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<Department> departments = await dbContext.Departments
            .OrderBy(department => department.Id)
            .ToListAsync(cancellationToken);

        Department registration = UpsertDepartment(
            departments,
            "Registratūros skyrius",
            "Bendrosios registracijos, specialistų registracijos ir pacientų priėmimo langeliai.");
        Department laboratory = UpsertDepartment(
            departments,
            "Laboratorijos skyrius",
            "Laboratorinių tyrimų registracija ir tyrimų atsakymų atsiėmimas.");
        Department documents = UpsertDepartment(
            departments,
            "Dokumentų skyrius",
            "Dokumentų, pažymų, siuntimų ir draudimo dokumentų tvarkymas.");
        Department administration = UpsertDepartment(
            departments,
            "Administracinių paslaugų skyrius",
            "Mišrios administracinės paslaugos ir pagalba apkrautiems langeliams.");

        await dbContext.SaveChangesAsync(cancellationToken);
        return new List<Department> { registration, laboratory, documents, administration };

        Department UpsertDepartment(List<Department> existingDepartments, string name, string description)
        {
            Department? department = existingDepartments.FirstOrDefault(item => item.Name == name);
            if (department == null)
            {
                department = new Department
                {
                    Name = name,
                    CreatedAt = DateTime.UtcNow
                };
                dbContext.Departments.Add(department);
                existingDepartments.Add(department);
            }

            department.Description = description;
            department.IsActive = true;
            return department;
        }
    }

    private static async Task<List<ServiceType>> EnsureServiceTypesAsync(AppDbContext dbContext, CancellationToken cancellationToken)
    {
        List<ServiceType> serviceTypes = await dbContext.ServiceTypes
            .OrderBy(serviceType => serviceType.Prefix)
            .ToListAsync(cancellationToken);

        ServiceType serviceA = UpsertServiceType(serviceTypes, "A", "Bendroji registracija", 8, 3, false, 30);
        ServiceType serviceB = UpsertServiceType(serviceTypes, "B", "Registracija pas specialistą", 10, 4, false, 25);
        ServiceType serviceC = UpsertServiceType(serviceTypes, "C", "Dokumentų pridavimas ir pažymų atsiėmimas", 4, 2, true, 15);
        ServiceType serviceD = UpsertServiceType(serviceTypes, "D", "Laboratorinių tyrimų registracija", 7, 4, false, 20);
        ServiceType serviceE = UpsertServiceType(serviceTypes, "E", "Tyrimų atsakymų atsiėmimas", 3, 2, true, 15);
        ServiceType serviceF = UpsertServiceType(serviceTypes, "F", "Siuntimų ir draudimo dokumentų tvarkymas", 6, 3, false, 20);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new List<ServiceType> { serviceA, serviceB, serviceC, serviceD, serviceE, serviceF };

        ServiceType UpsertServiceType(
            List<ServiceType> existingServiceTypes,
            string prefix,
            string name,
            int estimatedDurationMinutes,
            int priorityLevel,
            bool isFastTrack,
            int maxWaitMinutes)
        {
            ServiceType? serviceType = existingServiceTypes.FirstOrDefault(item => item.Prefix == prefix);
            if (serviceType == null)
            {
                serviceType = new ServiceType { Prefix = prefix };
                dbContext.ServiceTypes.Add(serviceType);
                existingServiceTypes.Add(serviceType);
            }

            serviceType.Name = name;
            serviceType.Description = name;
            serviceType.EstimatedDurationMinutes = estimatedDurationMinutes;
            serviceType.IsActive = true;
            serviceType.PriorityLevel = priorityLevel;
            serviceType.IsFastTrack = isFastTrack;
            serviceType.MaxWaitMinutes = maxWaitMinutes;

            return serviceType;
        }
    }

    private static async Task<List<Counter>> EnsureCountersAsync(
        AppDbContext dbContext,
        List<Department> departments,
        CancellationToken cancellationToken)
    {
        List<CounterSeed> counterSeeds = new List<CounterSeed>
        {
            new(1, "1 langelis", "Registratūros skyrius", CounterStatus.Active),
            new(2, "2 langelis", "Registratūros skyrius", CounterStatus.Active),
            new(3, "3 langelis", "Registratūros skyrius", CounterStatus.Active),
            new(4, "4 langelis", "Registratūros skyrius", CounterStatus.Paused),
            new(5, "5 langelis", "Dokumentų skyrius", CounterStatus.Active),
            new(6, "6 langelis", "Dokumentų skyrius", CounterStatus.Active),
            new(7, "7 langelis", "Dokumentų skyrius", CounterStatus.Active),
            new(8, "8 langelis", "Laboratorijos skyrius", CounterStatus.Active),
            new(9, "9 langelis", "Laboratorijos skyrius", CounterStatus.Active),
            new(10, "10 langelis", "Laboratorijos skyrius", CounterStatus.Closed),
            new(11, "11 langelis", "Administracinių paslaugų skyrius", CounterStatus.Active),
            new(12, "12 langelis", "Administracinių paslaugų skyrius", CounterStatus.Active)
        };

        Dictionary<string, Department> departmentsByName = departments.ToDictionary(department => department.Name);
        List<Counter> counters = await dbContext.Counters
            .OrderBy(counter => counter.Id)
            .ToListAsync(cancellationToken);
        List<Counter> seededCounters = new List<Counter>();

        for (int index = 0; index < counterSeeds.Count; index++)
        {
            CounterSeed seed = counterSeeds[index];
            Counter? counter = counters.FirstOrDefault(item => item.Name == seed.Name);

            if (counter == null && index < counters.Count && counters[index].Name.StartsWith($"{seed.DisplayOrder} ", StringComparison.OrdinalIgnoreCase))
            {
                counter = counters[index];
            }

            if (counter == null)
            {
                counter = new Counter();
                dbContext.Counters.Add(counter);
                counters.Add(counter);
            }

            counter.Name = seed.Name;
            counter.DisplayOrder = seed.DisplayOrder;
            counter.DepartmentId = departmentsByName[seed.DepartmentName].Id;
            counter.Status = seed.Status;
            seededCounters.Add(counter);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return seededCounters
            .OrderBy(counter => counter.DisplayOrder)
            .ToList();
    }

    private static async Task EnsureCounterServiceTypesAsync(
        AppDbContext dbContext,
        List<Counter> counters,
        List<ServiceType> serviceTypes,
        CancellationToken cancellationToken)
    {
        Dictionary<string, ServiceType> serviceTypesByPrefix = serviceTypes.ToDictionary(serviceType => serviceType.Prefix);
        List<CounterServiceSeed> seeds = new List<CounterServiceSeed>
        {
            new(1, new[] { "A", "B", "F" }, new[] { "A", "B" }),
            new(2, new[] { "A", "B", "F" }, new[] { "A", "B" }),
            new(3, new[] { "A", "B", "F" }, new[] { "A", "B" }),
            new(4, new[] { "A", "B", "C" }, new[] { "A" }),
            new(5, new[] { "C", "F", "E" }, new[] { "C", "F" }),
            new(6, new[] { "C", "F", "E" }, new[] { "C", "F" }),
            new(7, new[] { "C", "F", "E" }, new[] { "C", "F" }),
            new(8, new[] { "D", "E" }, new[] { "D", "E" }),
            new(9, new[] { "D", "E" }, new[] { "D", "E" }),
            new(10, new[] { "D", "E" }, new[] { "D" }),
            new(11, new[] { "A", "C", "F", "E" }, new[] { "C", "F" }),
            new(12, new[] { "A", "C", "F", "E" }, new[] { "C", "F" })
        };

        Dictionary<int, Counter> countersByDisplayOrder = counters.ToDictionary(counter => counter.DisplayOrder);
        List<int> counterIds = counters.Select(counter => counter.Id).ToList();
        List<CounterServiceType> existingLinks = await dbContext.CounterServiceTypes
            .Where(link => counterIds.Contains(link.CounterId))
            .ToListAsync(cancellationToken);

        HashSet<(int CounterId, int ServiceTypeId)> desiredKeys = seeds
            .SelectMany(seed =>
                seed.SupportedPrefixes.Select(prefix => (
                    CounterId: countersByDisplayOrder[seed.CounterDisplayOrder].Id,
                    ServiceTypeId: serviceTypesByPrefix[prefix].Id)))
            .ToHashSet();

        dbContext.CounterServiceTypes.RemoveRange(existingLinks
            .Where(link => !desiredKeys.Contains((link.CounterId, link.ServiceTypeId))));

        foreach (CounterServiceSeed seed in seeds)
        {
            Counter counter = countersByDisplayOrder[seed.CounterDisplayOrder];
            HashSet<string> enabledPrefixes = seed.EnabledPrefixes.ToHashSet();

            foreach (string prefix in seed.SupportedPrefixes)
            {
                ServiceType serviceType = serviceTypesByPrefix[prefix];
                CounterServiceType? existingLink = existingLinks.FirstOrDefault(link =>
                    link.CounterId == counter.Id &&
                    link.ServiceTypeId == serviceType.Id);

                if (existingLink == null)
                {
                    dbContext.CounterServiceTypes.Add(new CounterServiceType
                    {
                        CounterId = counter.Id,
                        ServiceTypeId = serviceType.Id,
                        IsEnabled = enabledPrefixes.Contains(prefix)
                    });
                }
                else
                {
                    existingLink.IsEnabled = enabledPrefixes.Contains(prefix);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<List<Operator>> EnsureOperatorsAsync(
        AppDbContext dbContext,
        List<Counter> counters,
        CancellationToken cancellationToken)
    {
        List<OperatorSeed> operatorSeeds = new List<OperatorSeed>
        {
            new(1, "Jonas Kazlauskas"),
            new(2, "Lina Petrauskienė"),
            new(3, "Mantas Stankevičius"),
            new(4, "Rūta Vaitkienė"),
            new(5, "Aistė Balčiūnaitė"),
            new(6, "Darius Paulauskas"),
            new(7, "Eglė Kavaliauskienė"),
            new(8, "Ieva Žukauskaitė"),
            new(9, "Tomas Jankauskas"),
            new(10, "Monika Šimkutė"),
            new(11, "Andrius Urbonas"),
            new(12, "Kristina Sakalauskaitė")
        };

        Dictionary<int, Counter> countersByDisplayOrder = counters.ToDictionary(counter => counter.DisplayOrder);
        List<Operator> operators = await dbContext.Operators
            .OrderBy(operatorProfile => operatorProfile.Id)
            .ToListAsync(cancellationToken);
        List<Operator> seededOperators = new List<Operator>();

        for (int index = 0; index < operatorSeeds.Count; index++)
        {
            OperatorSeed seed = operatorSeeds[index];
            Counter counter = countersByDisplayOrder[seed.CounterDisplayOrder];
            Operator? operatorProfile = operators.FirstOrDefault(item => item.CounterId == counter.Id);

            if (operatorProfile == null && index < operators.Count)
            {
                operatorProfile = operators[index];
            }

            if (operatorProfile == null)
            {
                operatorProfile = new Operator();
                dbContext.Operators.Add(operatorProfile);
                operators.Add(operatorProfile);
            }

            operatorProfile.Name = seed.FullName;
            operatorProfile.CounterId = counter.Id;
            operatorProfile.IsActive = true;
            seededOperators.Add(operatorProfile);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return seededOperators;
    }

    private static async Task EnsureUsersAsync(
        AppDbContext dbContext,
        List<Operator> operators,
        List<Department> departments,
        CancellationToken cancellationToken)
    {
        PasswordHasher<AppUser> hasher = new PasswordHasher<AppUser>();
        Dictionary<string, Department> departmentsByName = departments.ToDictionary(department => department.Name);

        AppUser adminUser = await EnsureUserAsync("admin", UserRole.Admin, null, AdminPassword, cancellationToken);
        adminUser.Role = UserRole.Admin;
        adminUser.OperatorId = null;

        List<ManagerSeed> managerSeeds = new List<ManagerSeed>
        {
            new("manager", new[] { "Registratūros skyrius" }),
            new("manager2", new[] { "Laboratorijos skyrius" }),
            new("manager3", new[] { "Dokumentų skyrius" }),
            new("manager4", new[] { "Administracinių paslaugų skyrius" }),
            new("labmanager", new[] { "Laboratorijos skyrius" }),
            new("documentsmanager", new[] { "Dokumentų skyrius" })
        };

        foreach (ManagerSeed seed in managerSeeds)
        {
            AppUser managerUser = await EnsureUserAsync(seed.Username, UserRole.Manager, null, ManagerPassword, cancellationToken);
            managerUser.Role = UserRole.Manager;
            managerUser.OperatorId = null;
            await dbContext.SaveChangesAsync(cancellationToken);

            await EnsureManagerDepartmentsAsync(
                dbContext,
                managerUser,
                seed.DepartmentNames.Select(name => departmentsByName[name]),
                cancellationToken);
        }

        for (int index = 0; index < operators.Count; index++)
        {
            string username = $"operator{index + 1}";
            AppUser appUser = await EnsureUserAsync(username, UserRole.Operator, operators[index].Id, OperatorPassword, cancellationToken);
            appUser.Role = UserRole.Operator;
            appUser.OperatorId = operators[index].Id;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        async Task<AppUser> EnsureUserAsync(
            string username,
            UserRole role,
            int? operatorId,
            string password,
            CancellationToken token)
        {
            AppUser? appUser = await dbContext.AppUsers
                .FirstOrDefaultAsync(user => user.Username == username, token);

            if (appUser == null)
            {
                appUser = new AppUser
                {
                    Username = username,
                    Role = role,
                    OperatorId = operatorId
                };
                appUser.PasswordHash = hasher.HashPassword(appUser, password); // Dev/demo-only credential.
                dbContext.AppUsers.Add(appUser);
            }

            return appUser;
        }
    }

    private static async Task EnsureManagerDepartmentsAsync(
        AppDbContext dbContext,
        AppUser manager,
        IEnumerable<Department> departments,
        CancellationToken cancellationToken)
    {
        List<int> departmentIds = departments.Select(department => department.Id).ToList();
        List<ManagerDepartment> existingLinks = await dbContext.ManagerDepartments
            .Where(link => link.AppUserId == manager.Id)
            .ToListAsync(cancellationToken);

        HashSet<int> existingDepartmentIds = existingLinks
            .Select(link => link.DepartmentId)
            .ToHashSet();

        foreach (int departmentId in departmentIds)
        {
            if (!existingDepartmentIds.Contains(departmentId))
            {
                dbContext.ManagerDepartments.Add(new ManagerDepartment
                {
                    AppUserId = manager.Id,
                    DepartmentId = departmentId
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureOperationalDemoTicketsAsync(
        AppDbContext dbContext,
        List<Counter> counters,
        List<Operator> operators,
        List<ServiceType> serviceTypes,
        CancellationToken cancellationToken)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        bool hasTicketsToday = await dbContext.Tickets
            .AnyAsync(ticket => DateOnly.FromDateTime(ticket.CreatedAt) == today, cancellationToken);

        if (hasTicketsToday)
        {
            return;
        }

        Dictionary<string, ServiceType> servicesByPrefix = serviceTypes.ToDictionary(serviceType => serviceType.Prefix);
        Dictionary<int, Counter> countersByDisplayOrder = counters.ToDictionary(counter => counter.DisplayOrder);
        Dictionary<int, Operator> operatorsByCounterId = operators.ToDictionary(operatorProfile => operatorProfile.CounterId);
        DateTime dayStart = DateTime.UtcNow.Date.AddHours(8);
        List<Ticket> tickets = new List<Ticket>();

        AddWaitingTickets("A", 6, dayStart.AddMinutes(10));
        AddWaitingTickets("B", 5, dayStart.AddMinutes(18));
        AddWaitingTickets("C", 5, dayStart.AddMinutes(25));
        AddWaitingTickets("D", 4, dayStart.AddMinutes(30));
        AddWaitingTickets("E", 4, dayStart.AddMinutes(35));
        AddWaitingTickets("F", 4, dayStart.AddMinutes(40));

        AddActiveTicket("A", "A007", TicketStatus.Called, 1, dayStart.AddMinutes(55), dayStart.AddMinutes(62));
        AddActiveTicket("B", "B006", TicketStatus.Called, 2, dayStart.AddMinutes(57), dayStart.AddMinutes(64));
        AddActiveTicket("D", "D005", TicketStatus.Serving, 8, dayStart.AddMinutes(50), dayStart.AddMinutes(58), dayStart.AddMinutes(62));
        AddActiveTicket("C", "C006", TicketStatus.Serving, 5, dayStart.AddMinutes(52), dayStart.AddMinutes(59), dayStart.AddMinutes(63));

        AddFinishedTicket("A", "A008", TicketStatus.Done, 3, dayStart.AddMinutes(5), dayStart.AddMinutes(14), dayStart.AddMinutes(22));
        AddFinishedTicket("B", "B007", TicketStatus.Done, 1, dayStart.AddMinutes(12), dayStart.AddMinutes(22), dayStart.AddMinutes(34));
        AddFinishedTicket("C", "C007", TicketStatus.Done, 6, dayStart.AddMinutes(20), dayStart.AddMinutes(28), dayStart.AddMinutes(36));
        AddFinishedTicket("D", "D006", TicketStatus.Done, 9, dayStart.AddMinutes(22), dayStart.AddMinutes(32), dayStart.AddMinutes(43));
        AddFinishedTicket("F", "F005", TicketStatus.Done, 11, dayStart.AddMinutes(26), dayStart.AddMinutes(35), dayStart.AddMinutes(45));

        AddFinishedTicket("A", "A009", TicketStatus.NoShow, 2, dayStart.AddMinutes(36), dayStart.AddMinutes(44), dayStart.AddMinutes(49));
        AddFinishedTicket("E", "E005", TicketStatus.NoShow, 8, dayStart.AddMinutes(38), dayStart.AddMinutes(47), dayStart.AddMinutes(52));
        AddFinishedTicket("F", "F006", TicketStatus.NoShow, 7, dayStart.AddMinutes(45), dayStart.AddMinutes(53), dayStart.AddMinutes(58));

        AddRecallPendingTicket("A", "A010", 1, dayStart.AddMinutes(30), dayStart.AddMinutes(70));

        AddRedirectedTicket("A", "C008", "C", 3, dayStart.AddMinutes(65), "Pasirinkta dokumentų paslauga vietoje registracijos.");
        AddRedirectedTicket("B", "F007", "F", 2, dayStart.AddMinutes(68), "Reikia tvarkyti siuntimo dokumentus.");
        AddRedirectedTicket("D", "E006", "E", 8, dayStart.AddMinutes(72), "Pacientui reikia tik tyrimų atsakymų atsiėmimo.");

        dbContext.Tickets.AddRange(tickets);
        await dbContext.SaveChangesAsync(cancellationToken);

        List<TicketEvent> events = new List<TicketEvent>();
        List<TicketRedirect> redirects = new List<TicketRedirect>();

        foreach (Ticket ticket in tickets)
        {
            events.Add(CreateEvent(ticket, TicketEventType.Created, null, TicketStatus.Waiting, ticket.CreatedAt, "Demo ticket created."));

            if (ticket.Status == TicketStatus.Called || ticket.Status == TicketStatus.Serving || ticket.Status == TicketStatus.Done || ticket.Status == TicketStatus.NoShow)
            {
                events.Add(CreateEvent(ticket, TicketEventType.Called, TicketStatus.Waiting, TicketStatus.Called, ticket.CalledAt ?? ticket.CreatedAt, "Demo ticket called."));
            }

            if (ticket.Status == TicketStatus.Serving || ticket.Status == TicketStatus.Done)
            {
                events.Add(CreateEvent(ticket, TicketEventType.ServingStarted, TicketStatus.Called, TicketStatus.Serving, ticket.ServedAt ?? ticket.CalledAt ?? ticket.CreatedAt, "Demo service started."));
            }

            if (ticket.Status == TicketStatus.Done)
            {
                events.Add(CreateEvent(ticket, TicketEventType.Completed, TicketStatus.Serving, TicketStatus.Done, ticket.ServedAt ?? ticket.CreatedAt, "Demo ticket completed."));
            }

            if (ticket.Status == TicketStatus.NoShow)
            {
                events.Add(CreateEvent(ticket, TicketEventType.NoShow, TicketStatus.Called, TicketStatus.NoShow, ticket.ServedAt ?? ticket.CreatedAt, "Demo ticket marked no-show."));
            }

            if (ticket.Status == TicketStatus.RecallPending)
            {
                events.Add(CreateEvent(ticket, TicketEventType.RecallPending, TicketStatus.NoShow, TicketStatus.RecallPending, ticket.RecallRequestedAt ?? ticket.CreatedAt, "Demo recall is waiting for the current ticket to finish."));
            }
        }

        foreach (Ticket ticket in tickets.Where(ticket => ticket.TicketNumber is "C008" or "F007" or "E006"))
        {
            string fromPrefix = ticket.TicketNumber switch
            {
                "C008" => "A",
                "F007" => "B",
                _ => "D"
            };
            ServiceType fromService = servicesByPrefix[fromPrefix];
            Operator operatorProfile = operatorsByCounterId[ticket.CounterId ?? countersByDisplayOrder[1].Id];
            redirects.Add(new TicketRedirect
            {
                TicketId = ticket.Id,
                FromServiceTypeId = fromService.Id,
                ToServiceTypeId = ticket.ServiceTypeId,
                FromCounterId = ticket.CounterId,
                RedirectedByOperatorId = operatorProfile.Id,
                Reason = ticket.TicketNumber switch
                {
                    "C008" => "Pasirinkta dokumentų paslauga vietoje registracijos.",
                    "F007" => "Reikia tvarkyti siuntimo dokumentus.",
                    _ => "Pacientui reikia tik tyrimų atsakymų atsiėmimo."
                },
                CreatedAt = ticket.QueueEnteredAt
            });
            events.Add(CreateEvent(ticket, TicketEventType.Redirected, TicketStatus.Serving, TicketStatus.Waiting, ticket.QueueEnteredAt, "Demo ticket redirected.", fromService.Id, ticket.ServiceTypeId));
            ticket.CounterId = null;
            ticket.Status = TicketStatus.Waiting;
            ticket.CalledAt = null;
            ticket.ServedAt = null;
        }

        dbContext.TicketRedirects.AddRange(redirects);
        dbContext.TicketEvents.AddRange(events);
        await dbContext.SaveChangesAsync(cancellationToken);

        void AddWaitingTickets(string prefix, int count, DateTime start)
        {
            ServiceType service = servicesByPrefix[prefix];
            for (int index = 1; index <= count; index++)
            {
                DateTime createdAt = start.AddMinutes(index * 3);
                tickets.Add(new Ticket
                {
                    ServiceTypeId = service.Id,
                    TicketNumber = $"{prefix}{index:D3}",
                    Status = TicketStatus.Waiting,
                    CreatedAt = createdAt,
                    QueueEnteredAt = createdAt
                });
            }
        }

        void AddActiveTicket(
            string prefix,
            string ticketNumber,
            TicketStatus status,
            int counterDisplayOrder,
            DateTime createdAt,
            DateTime calledAt,
            DateTime? servedAt = null)
        {
            tickets.Add(new Ticket
            {
                ServiceTypeId = servicesByPrefix[prefix].Id,
                TicketNumber = ticketNumber,
                Status = status,
                CounterId = countersByDisplayOrder[counterDisplayOrder].Id,
                CreatedAt = createdAt,
                QueueEnteredAt = createdAt,
                CalledAt = calledAt,
                ServedAt = servedAt
            });
        }

        void AddFinishedTicket(
            string prefix,
            string ticketNumber,
            TicketStatus status,
            int counterDisplayOrder,
            DateTime createdAt,
            DateTime calledAt,
            DateTime finishedAt)
        {
            tickets.Add(new Ticket
            {
                ServiceTypeId = servicesByPrefix[prefix].Id,
                TicketNumber = ticketNumber,
                Status = status,
                CounterId = countersByDisplayOrder[counterDisplayOrder].Id,
                CreatedAt = createdAt,
                QueueEnteredAt = createdAt,
                CalledAt = calledAt,
                ServedAt = finishedAt
            });
        }

        void AddRecallPendingTicket(string prefix, string ticketNumber, int counterDisplayOrder, DateTime createdAt, DateTime recallRequestedAt)
        {
            tickets.Add(new Ticket
            {
                ServiceTypeId = servicesByPrefix[prefix].Id,
                TicketNumber = ticketNumber,
                Status = TicketStatus.RecallPending,
                CounterId = countersByDisplayOrder[counterDisplayOrder].Id,
                CreatedAt = createdAt,
                QueueEnteredAt = createdAt,
                CalledAt = createdAt.AddMinutes(8),
                ServedAt = createdAt.AddMinutes(14),
                RecallRequestedAt = recallRequestedAt
            });
        }

        void AddRedirectedTicket(string fromPrefix, string ticketNumber, string toPrefix, int fromCounterDisplayOrder, DateTime createdAt, string reason)
        {
            _ = reason;
            tickets.Add(new Ticket
            {
                ServiceTypeId = servicesByPrefix[toPrefix].Id,
                TicketNumber = ticketNumber,
                Status = TicketStatus.Serving,
                CounterId = countersByDisplayOrder[fromCounterDisplayOrder].Id,
                CreatedAt = createdAt,
                QueueEnteredAt = createdAt.AddMinutes(12),
                CalledAt = createdAt.AddMinutes(4),
                ServedAt = createdAt.AddMinutes(8)
            });
        }

        TicketEvent CreateEvent(
            Ticket ticket,
            TicketEventType eventType,
            TicketStatus? previousStatus,
            TicketStatus? newStatus,
            DateTime createdAt,
            string details,
            int? fromServiceTypeId = null,
            int? toServiceTypeId = null)
        {
            int? operatorId = ticket.CounterId.HasValue && operatorsByCounterId.TryGetValue(ticket.CounterId.Value, out Operator? operatorProfile)
                ? operatorProfile.Id
                : null;

            return new TicketEvent
            {
                TicketId = ticket.Id,
                TicketNumber = ticket.TicketNumber,
                EventType = eventType,
                OperatorId = operatorId,
                CounterId = ticket.CounterId,
                ServiceTypeId = ticket.ServiceTypeId,
                FromServiceTypeId = fromServiceTypeId,
                ToServiceTypeId = toServiceTypeId,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                CreatedAt = createdAt,
                Details = details
            };
        }
    }

    private sealed record CounterSeed(int DisplayOrder, string Name, string DepartmentName, CounterStatus Status);
    private sealed record CounterServiceSeed(int CounterDisplayOrder, string[] SupportedPrefixes, string[] EnabledPrefixes);
    private sealed record OperatorSeed(int CounterDisplayOrder, string FullName);
    private sealed record ManagerSeed(string Username, string[] DepartmentNames);
}
