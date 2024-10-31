package io.github.xkitsios.util;

import java.sql.*;
import java.util.ArrayList;
import java.io.FileWriter;

import io.github.xkitsios.Point;

public class DatabaseRepository {
    public static TimeSeries getTimeSeriesFromDatabase(String parameter, String device) throws Exception {
        String connectionUrl = "";
        Connection connection = DriverManager.getConnection(connectionUrl);

        PreparedStatement preparedStatement = connection.prepareStatement("SELECT value FROM " +
            "\"observations-src\" WHERE parameter = ? AND source = ? ORDER BY Time ASC");
        preparedStatement.setString(1, parameter);
        preparedStatement.setString(2, device);

        ResultSet resultSet = preparedStatement.executeQuery();

        // FileWriter fileWriter = new FileWriter(parameter + " - " + device + ".csv");

        ArrayList<Point> pointList = new ArrayList<Point>();

        long timestamp = 0;
        double timeSeriesMaximum = Double.NEGATIVE_INFINITY;
        double timeSeriesMinimum = Double.POSITIVE_INFINITY;

        while (resultSet.next()) {
            try {
                double value = resultSet.getDouble(1);
            
                // System.out.println("Timestamp: " + timestamp + ", Value: " + value);

                // fileWriter.write(timestamp + "," + value + "\n");

                timeSeriesMaximum = Math.max(timeSeriesMaximum, value);
                timeSeriesMinimum = Math.min(timeSeriesMinimum, value);

                pointList.add(new Point(timestamp, value));
    
                timestamp++;
            }
            catch (Exception exception) {
                System.err.println(exception.getMessage());
            }
        }

        double timeSeriesRange = timeSeriesMaximum - timeSeriesMinimum;
        System.out.println("Number of data points: " + timestamp);
        System.out.println("Time series range: " + timeSeriesRange);

        // fileWriter.close();
        resultSet.close();
        preparedStatement.close();

        return new TimeSeries(pointList, timeSeriesRange);
    }
}
