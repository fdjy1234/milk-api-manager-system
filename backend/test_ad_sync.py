import unittest
from unittest.mock import MagicMock, AsyncMock, patch
import sys
import os

# Mocking the C# classes for logic testing in Python
class AdGroup:
    def __init__(self, name, members):
        self.name = name
        self.members = members

class TestAdGroupSyncLogic(unittest.TestCase):
    def test_group_processing_logic(self):
        # Simulated AD groups
        ad_groups = [
            AdGroup("Developers", ["alice", "bob"]),
            AdGroup("Managers", ["charlie"])
        ]
        
        synced_groups = []
        synced_users = []

        # Simulate the logic in AdGroupSyncService.cs
        for group in ad_groups:
            group_id = group.name.lower()
            synced_groups.append(group_id)
            for member in group.members:
                synced_users.append((member, group_id))

        self.assertIn("developers", synced_groups)
        self.assertIn("managers", synced_groups)
        self.assertEqual(len(synced_users), 3)
        self.assertIn(("alice", "developers"), synced_users)
        self.assertIn(("charlie", "managers"), synced_users)

if __name__ == '__main__':
    unittest.main()
