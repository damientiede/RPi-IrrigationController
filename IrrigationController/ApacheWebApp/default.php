<?php 
   echo date('Y-m-d H:i:s'); 
   $conn = new mysqli("localhost","root","raspberry","IrrigationController");
   if ($conn->connect_error) {
      die("Connection failed " . $conn->connect_error);
   }

   $sql = "SELECT * from EventHistory where Id > 1000";
   $result = $conn->query($sql);

   if ($result->num_rows > 0) {
      while($row = $result->fetch_assoc()) {
         echo  "TStamp: " . $row["TStamp"]. "- Desc: " . $row["Description"] . "<br/>";
      }
   }
   else
   {
      echo "0 results";
   }
   $conn->close();



?>

