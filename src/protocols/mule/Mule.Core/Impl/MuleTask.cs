//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using Mule.File;

//namespace Mule.Core.Impl
//{
//    class MuleTask
//    {
//        AbstractFile abstractFile_ = null;

//        public void LoadComment()
//        {
//            abstractFile_.FileComment = MuleEngine.CoreObjectManager.Preference.GetFileComment(abstractFile_.FileHash);

//            abstractFile_.FileRating = MuleEngine.CoreObjectManager.Preference.GetFileRating(abstractFile_.FileHash);
//        }

//        public bool AddNote(Kademlia.KadEntry pEntry)
//        {
//            foreach (Kademlia.KadEntry entry in kad_entry_notes_)
//            {
//                if (entry.SourceID.Equals(pEntry.SourceID))
//                {
//                    return false;
//                }
//            }

//            kad_entry_notes_.Insert(0, pEntry);

//            UpdateFileRatingCommentAvail();

//            return true;
//        }

//        public void RefilterKadNotes()
//        {
//            RefilterKadNotes(true);
//        }

//        public void RefilterKadNotes(bool bUpdate)
//        {
//            // check all availabe comments against our filter again
//            if (string.IsNullOrEmpty(MuleEngine.CoreObjectManager.Preference.CommentFilter))
//            {
//                return;
//            }

//            KadEntryList removed = new KadEntryList();

//            string[] filters =
//                MuleEngine.CoreObjectManager.Preference.CommentFilter.Split('|');

//            if (filters == null || filters.Length == 0)
//                return;

//            foreach (KadEntry entry in kad_entry_notes_)
//            {
//                string desc =
//                    entry.GetStrTagValue(MuleConstants.TAG_DESCRIPTION);

//                if (!string.IsNullOrEmpty(desc))
//                {
//                    string strCommentLower = desc.ToLower();

//                    foreach (string filter in filters)
//                    {
//                        if (strCommentLower.IndexOf(filter) >= 0)
//                        {
//                            removed.Add(entry);
//                            break;
//                        }
//                    }
//                }
//            }

//            foreach (KadEntry entry in removed)
//                kad_entry_notes_.Remove(entry);

//            // untill updated rating and m_bHasComment might be wrong
//            if (bUpdate)
//            {
//                UpdateFileRatingCommentAvail();
//            }
//        }

//        bool IsKadCommentSearchRunning { get; set; }

//        public void UpdateFileRatingCommentAvail(bool bForceUpdate)
//        {
//            bool bOldHasComment = HasComment;
//            uint uOldUserRatings = UserRating;

//            HasComment = false;
//            uint uRatings = 0;
//            uint uUserRatings = 0;

//            foreach (KadEntry entry in KadNotes)
//            {
//                string desc = entry.GetStrTagValue(MuleConstants.TAG_DESCRIPTION);

//                if (!HasComment && !string.IsNullOrEmpty(desc))
//                    HasComment = true;
//                uint rating = Convert.ToUInt32(entry.GetIntTagValue(MuleConstants.TAG_FILERATING));

//                if (rating != 0)
//                {
//                    uRatings++;
//                    uUserRatings += rating;
//                }
//            }

//            if (uRatings > 0)
//                UserRating = Convert.ToUInt32(Math.Round((float)uUserRatings / (float)uRatings));
//            else
//                UserRating = 0;

//            if (bOldHasComment != HasComment ||
//                uOldUserRatings != UserRating ||
//                bForceUpdate)
//            {
//                //TODO: File Event which File comments / user rating changes
//            }
//        }

//    }
//}
