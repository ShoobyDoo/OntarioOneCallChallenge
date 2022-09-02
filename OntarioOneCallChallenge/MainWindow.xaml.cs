using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Xaml;
using MySql.Data.MySqlClient;

/******************************************************************************
 * Author:          Shoaib Ali
 * Created:         08/22/2022
 * Last Edited:     08/28/2022
 * Description:     Main application logic for the Ontario One Call Challenge.
 ******************************************************************************/

namespace OntarioOneCallChallenge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Master list of all current node selections as program is running
        private List<string> myNodeSelections = new List<string>();

        // Create a list of compliance objects which will the the itemsource for datagrid
        private List<Compliance> compliances = new List<Compliance>();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                // Assign item source right away to establish column headers
                dgCompliance.ItemsSource = compliances;

                // Init database connection, then pass through connection string
                // For the sake of the challenge, there is no password for the user root
                MySqlConnection dbConnection = new MySqlConnection();
                string myConnectionString = "server=localhost;uid=root;pwd=;database=dbOntarioOneTest;";
                dbConnection.ConnectionString = myConnectionString;
                
                // Create a query command to retreive all the records in the master station code table
                // These records will be required to construct the tree view
                string myGetNodesQuery = "SELECT master_station_code, member_code FROM tblmasterstationcode;";
                MySqlCommand myCommand = new MySqlCommand(myGetNodesQuery, dbConnection);
                
                // Open the connection and initialize a reader object since we will be receiving data from the db
                dbConnection.Open();
                MySqlDataReader reader = myCommand.ExecuteReader();

                // Create a list of string lists to store the master_station_code and member_codes
                // Ex: outer main list -> { inner lists -> {master_station_code, member_code}, {""}, {""}... }
                List<List<string>> myCodesList = new List<List<string>>();

                // We read recursively from the reader as any one member code could have multiple tickets/results
                while (reader.Read())
                {
                    // Append those results into the list of codes
                    myCodesList.Add(new List<string>() { reader.GetString("master_station_code"), reader.GetString("member_code") });
                }

                // Close the connection
                myCommand.Connection.Close();

                // Create a treenode reference, so that we can refer back to parent index when assigning child nodes
                System.Windows.Forms.TreeNode tNode = new System.Windows.Forms.TreeNode();

                // Create a string to keep track of the previous node in the loop
                string previousNode = "";
                foreach (List<string> stringList in myCodesList)
                {
                    // If current node is the same as the previous, so for example OOCTEST1 == OOCTEST1
                    // then the member_code is a child node. also for ref, stringlist[0] is the master code/parent
                    if (previousNode == stringList[0])
                    {
                        // Access the index of the tNode ref (parent) and add as a child
                        tvNodes.Nodes[tvNodes.Nodes.IndexOf(tNode)].Nodes.Add(stringList[1]);
                    }
                    else
                    {
                        // If the current node is not the same as the previous, this is likely because
                        // the value is a new parent; so we just add it to the top
                        tNode = tvNodes.Nodes.Add(stringList[0]);

                        // We still need to add this because even in the excel sheet, the record next to OOCTEST1
                        // for example is still its child
                        tvNodes.Nodes[tvNodes.Nodes.IndexOf(tNode)].Nodes.Add(stringList[1]);
                    }

                    // This iteration is complete, update previous node 
                    previousNode = stringList[0];
                }

                // Once we get all the nodes with no errors, we update the status text to something meaningful
                tbStatus.Text = String.Format("Application loaded successfully; Database connected, tree contains {0} parent nodes.", tvNodes.Nodes.Count.ToString());
            }
            catch (MySqlException ex)
            {
                // We catch a general MySQLException and update the status text to let the user know, as well as opening a simple mbox
                // with the error msg.
                tbStatus.Text = "[!] Database error, see message box for details. Have you run the setup script? InstallDir/Setup/easy-install.bat";
                MessageBox.Show("ERROR!\n\n" + ex.Message);
            }
        }

        /// <summary>
        /// Updates all child tree nodes recursively.
        /// </summary>
        /// <param name="treeNode">Parent node which was selected</param>
        /// <param name="nodeChecked">Nodes checked property parameter</param>
        /// <see href="https://docs.microsoft.com/en-us/dotnet/api/system.windows.forms.treeview.aftercheck?redirectedfrom=MSDN&view=netframework-4.8"/>
        private void CheckAllChildNodes(System.Windows.Forms.TreeNode treeNode, bool nodeChecked)
        {
            foreach (System.Windows.Forms.TreeNode node in treeNode.Nodes)
            {
                node.Checked = nodeChecked;
                if (node.Nodes.Count > 0)
                {
                    // If the current node has child nodes, call the CheckAllChildsNodes method recursively.
                    this.CheckAllChildNodes(node, nodeChecked);
                }
            }
        }

        /// <summary>
        /// Event handler that captures after a node is checked.
        /// </summary>
        private void tvNodes_AfterCheck(object sender, System.Windows.Forms.TreeViewEventArgs e)
        {
            // The code only executes if the user caused the checked state to change.
            if (e.Action != System.Windows.Forms.TreeViewAction.Unknown)
            {
                if (e.Node.Nodes.Count > 0)
                {
                    /* Calls the CheckAllChildNodes method, passing in the current 
                    Checked value of the TreeNode whose checked state changed. */
                    this.CheckAllChildNodes(e.Node, e.Node.Checked);
                }
            }

            // Extract the parent node from the full path
            string parentNode = e.Node.FullPath.Split(tvNodes.PathSeparator.ToCharArray())[0]; // 0=parent, 1=self

            // Check to see if the currently selected node does not already exist in the master node selection list
            // and make sure that the node is not a parent node, we append to the master list and update the status
            if (!myNodeSelections.Contains(e.Node.Text) && e.Node.Text != parentNode)
            {
                myNodeSelections.Add(e.Node.Text);
                tbStatus.Text = String.Format("Selection updated -> {0} (ADD)", e.Node.Text);
            }
            // If the selection was the parent node, only update the status message
            else if (e.Node.Text == parentNode && e.Node.Checked == true)
            {
                tbStatus.Text = String.Format("Selection updated -> All child nodes of {0} (ADD)", e.Node.Text);
            }
            else if (e.Node.Text == parentNode && e.Node.Checked == false)
            {
                tbStatus.Text = String.Format("Selection updated -> All child nodes of {0} (REMOVE)", e.Node.Text);
            }
            // Check to see if the current selection already exists in the list; meaning it was already selected
            // and then check to see if its current status is unchecked, meaning the user unselected it so remove
            // it from the main list and update status message
            else if (myNodeSelections.Contains(e.Node.Text) && e.Node.Checked == false)
            {
                myNodeSelections.Remove(e.Node.Text);
                tbStatus.Text = String.Format("Selection updated -> {0} (REMOVE)", e.Node.Text);
            }

            // A string builder for the current selection header
            string currentSelection = "";
            myNodeSelections.Sort(); // Sort the list so OOC001 is always before OOC002, etc. regardless of the order they are un/checked
            foreach (string node in myNodeSelections)
            {
                if (node != parentNode)
                {
                    currentSelection += node + ", ";
                }
            }

            // Finally, once all strings are built and ready, update the labels
            lblParentChildHeader.Text = String.Format("Node Full Path: {0}\\{1}", parentNode, currentSelection);
            lblCurrentSelection.Text = String.Format("Current Selection: {0}", currentSelection);
        }

        /// <summary>
        /// Event handler for btnExit; which closes the program.
        /// </summary>
        private void btnExit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Event handler for btnPopulateData; logic that handles stored procedure
        /// </summary>
        private void btnPopulateData_Click(object sender, RoutedEventArgs e)
        {
            // Clear the compliances list before each run to ensure the datagrid does not
            // feed into itself (clears the previous entry, if there was one)
            compliances.Clear();

            // Check to see if the user even selected any nodes
            // If not, we can early return and update the status message accordingly
            if (myNodeSelections.Count == 0)
            {
                tbStatus.Text = String.Format("No selection(s) to populate!", myNodeSelections.Count.ToString());
                return;
            }

            // Update the status message
            tbStatus.Text = String.Format("Populating table passing {0} selection(s)...", myNodeSelections.Count.ToString());

            // Create a temporary dictionary to hold the store procedure response
            Dictionary<string, int> spResponse = new Dictionary<string, int>();

            // Some temporary counters
            int totalCompliance = 0; // overall total
            int totalCompliant = 0; // total compliant only
            int totalNonCompliant = 0; // total non-compliant only

            // Loop through each of the master node selections
            foreach (string node in myNodeSelections)
            {
                // Init database connection, then pass through connection string
                // Extra remarks: Same as before, we init connection, open and feed our command to db
                MySqlConnection dbConnection = new MySqlConnection();
                string myConnectionString = "server=localhost;uid=root;pwd=;database=dbOntarioOneTest;";
                dbConnection.ConnectionString = myConnectionString;
                dbConnection.Open();

                // Instead of writing a query to call the stored procedure, we can specify the command type
                MySqlCommand myCommand = new MySqlCommand("sp_OntarioOneCallChallenge", dbConnection);
                myCommand.CommandType = System.Data.CommandType.StoredProcedure;

                // Here we pass in the 'node' parameter to 'IN member varchar...' in the stored procedure
                myCommand.Parameters.AddWithValue("member", node);

                // We initialize the reader and start reading the output
                MySqlDataReader reader = myCommand.ExecuteReader();
                while (reader.Read())
                {
                    // If the ticket was compliant
                    if (reader.GetString("compliance") == "Compliant")
                    {
                        // If one of the parameters exist already in the list, we just append to its 'counter'
                        if (spResponse.ContainsKey(reader.GetString("time_to_respond")))
                        {
                            // if time to respond does exist, add to its compliant value
                            spResponse[reader.GetString("time_to_respond")] += 1;
                        }
                        else
                        {
                            // if time to respond doesnt exist, create it with value 1
                            spResponse.Add(reader.GetString("time_to_respond"), 1);
                        }

                        // Then just increment the overall total
                        totalCompliant += 1;
                    }
                    else
                    {
                        if (spResponse.ContainsKey(reader.GetString("time_to_respond")))
                        {
                            // if time to respond does exist, add to its compliant value
                            spResponse[reader.GetString("time_to_respond")] += 1;
                        }
                        else
                        {
                            // if time to respond doesnt exist, create it with value 1
                            spResponse.Add(reader.GetString("time_to_respond"), 1);
                        }
                        totalNonCompliant += 1;
                    }

                    // running total
                    totalCompliance += 1;
                }
                myCommand.Connection.Close(); // Close the conn
            }

            // Now that we have all our values, we can properly construct the Compliant objects
            foreach (KeyValuePair<string, int> kvp in spResponse)
            {
                // Add Compliance objects to the compliances list
                compliances.Add(new Compliance()
                {
                    TimeToRespond = kvp.Key,
                    Percentage = String.Format("{0:P2}", (double)kvp.Value / (double)totalCompliance),
                    Compliant = kvp.Key == "0-5" ? kvp.Value.ToString() : "",
                    NonCompliant = kvp.Key != "0-5" ? kvp.Value.ToString() : ""
                });

                // If this iteration was the last kvp in the dictionary,
                // db response is over and we need to append the totals as compliance objects
                if (kvp.Equals(spResponse.Last()))
                {
                    List<Compliance> tmpSummary = new List<Compliance>() 
                    { 
                        new Compliance() { TimeToRespond = "_________________________", Percentage = "_________________________", Compliant = "_________________________", NonCompliant = "_________________________" },
                        new Compliance() { TimeToRespond = "Total", Percentage = "", Compliant = totalCompliant.ToString(), NonCompliant = totalNonCompliant.ToString() },
                        new Compliance() { TimeToRespond = "", Percentage = "", Compliant = String.Format("{0:P2}", (double)totalCompliant / (double)totalCompliance), NonCompliant = String.Format("{0:P2}", (double)totalNonCompliant / (double)totalCompliance) }
                    };

                    // Loop through those temporary summary compliance objects and
                    // append them to the data grid item source
                    foreach (Compliance summaryItem in tmpSummary)
                    {
                        compliances.Add(summaryItem);
                    }
                }
            }

            // Refresh so datagrid picks up the store procedure values
            dgCompliance.Items.Refresh(); 

            // Access the last and second last columns of the datagrid and change the color
            // and font weight to their respective bold, green and red for compliant and non-compliant
            dgCompliance.Columns[dgCompliance.Columns.Count - 1].CellStyle = (Style)XamlServices.Parse("<Style xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"{x:Type DataGridCell}\"> " +
                "<Setter Property=\"Background\" Value=\"IndianRed\"/> " +
                "<Setter Property=\"BorderThickness\" Value=\"0\"/>" +
                "<Setter Property=\"FontWeight\" Value=\"Bold\"/></Style>");
            dgCompliance.Columns[dgCompliance.Columns.Count - 2].CellStyle = (Style)XamlServices.Parse("<Style xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\" xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\" TargetType=\"{x:Type DataGridCell}\"> " +
                "<Setter Property=\"Background\" Value=\"LightGreen\"/> " +
                "<Setter Property=\"BorderThickness\" Value=\"0\"/>" +
                "<Setter Property=\"FontWeight\" Value=\"Bold\"/></Style>");

            // Finally, disable the button
            btnPopulateData.IsEnabled = false;
        }
    }

    /// <summary>
    /// Compliance class outlines the datagrid itemsource
    /// </summary>
    public class Compliance
    {
        public string TimeToRespond { get; set; }
        public string Percentage { get; set; }
        public string Compliant { get; set; }
        public string NonCompliant { get; set; }
    }
}
