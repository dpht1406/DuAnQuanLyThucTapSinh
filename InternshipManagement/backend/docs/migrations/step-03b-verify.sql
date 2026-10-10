-- Expected on the current database: Accepted 3, Pending 1, Rejected 17, Approved 0.

-- Q1. Count placement requests by status.
SELECT
    Status,
    COUNT(*) AS RequestCount
FROM PlacementRequests
GROUP BY Status
ORDER BY Status;

-- Q2. Requests mapped by this migration with the student's current company.
SELECT
    pr.Id AS PlacementRequestId,
    pr.StudentId,
    pr.Status,
    pr.CompanyId AS RequestCompanyId,
    s.CompanyId AS StudentCompanyId,
    pr.ConfirmationSource
FROM PlacementRequests AS pr
INNER JOIN Students AS s ON s.Id = pr.StudentId
WHERE pr.ConfirmationSource LIKE N'%(Approved%'
ORDER BY pr.Id;

-- Q3. Accepted requests whose student status needs manual admin review.
SELECT
    pr.Id AS PlacementRequestId,
    pr.StudentId,
    s.StudentCode,
    s.Status AS StudentStatus,
    s.CompanyId
FROM PlacementRequests AS pr
INNER JOIN Students AS s ON s.Id = pr.StudentId
WHERE pr.Status = N'Accepted'
    AND s.Status NOT IN (N'Accepted', N'Interning', N'Completed')
ORDER BY pr.Id;

-- Q4. Pending requests without a due date (expected 0).
SELECT COUNT(*) AS PendingWithoutDueAtCount
FROM PlacementRequests
WHERE Status = N'Pending'
    AND DueAt IS NULL;

-- Q5. Remaining Approved requests (expected 0).
SELECT COUNT(*) AS ApprovedRequestCount
FROM PlacementRequests
WHERE Status = N'Approved';
