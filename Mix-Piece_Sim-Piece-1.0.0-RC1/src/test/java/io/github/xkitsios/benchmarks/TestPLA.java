package io.github.xkitsios.benchmarks;

import io.github.xkitsios.MixPiece;
import io.github.xkitsios.Point;
import io.github.xkitsios.SimPiece;
import io.github.xkitsios.util.TimeSeries;
import io.github.xkitsios.util.TimeSeriesReader;
import io.github.xkitsios.util.DatabaseRepository;
import org.junit.jupiter.api.Test;

import java.util.List;

import static org.junit.jupiter.api.Assertions.assertEquals;

public class TestPLA {
    private long SimPiece(List<Point> ts, double epsilon) throws Exception {
        byte[] binary = SimPiece.compress(ts, epsilon);
        List<Point> tsDecompressed = SimPiece.decompress(binary);
        int idx = 0;
        for (Point expected : tsDecompressed) {
            Point actual = ts.get(idx);
            if (expected.getTimestamp() != actual.getTimestamp()) continue;
            idx++;
            assertEquals(actual.getValue(), expected.getValue(), 1.1 * epsilon, "Value did not match for timestamp " + actual.getTimestamp());
        }
        assertEquals(idx, ts.size());

        return binary.length;
    }

    private long MixPiece(List<Point> ts, double epsilon) throws Exception {
        byte[] binary = MixPiece.compress(ts, epsilon);
        List<Point> tsDecompressed = MixPiece.decompress(binary);
        int idx = 0;
        for (Point expected : tsDecompressed) {
            Point actual = ts.get(idx);
            if (expected.getTimestamp() != actual.getTimestamp()) continue;
            idx++;
            assertEquals(actual.getValue(), expected.getValue(), 1.1 * epsilon, "Value did not match for timestamp " + actual.getTimestamp());
        }
        assertEquals(idx, ts.size());

        return binary.length;
    }


    private void run(String[] filenames, double epsilonStart, double epsilonStep, double epsilonEnd, int bufferWindow) throws Exception {
        for (String filename : filenames) {
            System.out.println(filename);
            String delimiter = ",";
            TimeSeries ts = TimeSeriesReader.getTimeSeries(getClass().getResourceAsStream(filename), delimiter, true, bufferWindow);
            
            System.out.println("Number of points: " + ts.data.size());
            
            System.out.println("Mix-Piece");
            for (double epsilonPct = epsilonStart; epsilonPct <= epsilonEnd; epsilonPct += epsilonStep)
                System.out.printf("Epsilon: %.2f%%\tCompression Ratio: %.3f\n", epsilonPct * 100, (double) ts.size / MixPiece(ts.data, ts.range * epsilonPct));

            System.out.println("Sim-Piece");
            for (double epsilonPct = epsilonStart; epsilonPct <= epsilonEnd; epsilonPct += epsilonStep)
                System.out.printf("Epsilon: %.2f%%\tCompression Ratio: %.3f\n", epsilonPct * 100, (double) ts.size / SimPiece(ts.data, ts.range * epsilonPct));

            System.out.println();
        }
    }

    private void runWithDbDataset(String[][] parameters, double epsilonStart, double epsilonStep, double epsilonEnd) throws Exception {
        for (String[] tuple : parameters) {
            System.out.println("Parameter: " + tuple[0] + ", Sensor: " + tuple[1]);

            TimeSeries ts = DatabaseRepository.getTimeSeriesFromDatabase(tuple[0], tuple[1]);

            System.out.println("Mix-Piece");
            for (double epsilonPct = epsilonStart; epsilonPct <= epsilonEnd; epsilonPct += epsilonStep)
                System.out.printf("Epsilon: %.2f%%\tCompression Ratio: %.3f\n", epsilonPct * 100, (double) ts.size / MixPiece(ts.data, ts.range * epsilonPct));

            System.out.println("Sim-Piece");
            for (double epsilonPct = epsilonStart; epsilonPct <= epsilonEnd; epsilonPct += epsilonStep)
                System.out.printf("Epsilon: %.2f%%\tCompression Ratio: %.3f\n", epsilonPct * 100, (double) ts.size / SimPiece(ts.data, ts.range * epsilonPct));

            System.out.println();
        }
    }

    @Test
    public void TestCRAndTime() throws Exception {
        double epsilonStart = 0.005;
        double epsilonStep = 0.005;
        double epsilonEnd = 0.05;

        // String[] filenames = {
        //     "/Cricket.csv.gz",
        //     "/FaceFour.csv.gz",
        //     "/Lightning.csv.gz",
        //     "/MoteStrain.csv.gz",
        //     "/Wafer.csv.gz",
        //     "/WindSpeed.csv.gz",
        //     "/WindDirection.csv.gz",
        // };

        String[] filenames = {
            //"/Austevoll Data/Abs Tilt - DCPS #122.csv.gz",
            //"/Austevoll Data/Oxygen - SeaBird SBE #1111.csv.gz",
            //"/Austevoll Data/Temperature - Temperature Sensor #1063.csv.gz",
            "/Austevoll Data/Turbidity#16340 - Analog Sensors #0.csv.gz"
        };

        run(filenames, epsilonStart, epsilonStep, epsilonEnd, 8662);

        // epsilonStart = 0.0005;
        // epsilonStep = 0.0005;
        // epsilonEnd = 0.0051;

        // filenames = new String[]{
        //     "/Pressure.csv.gz",
        //     "/BTCUSD.csv.gz",
        //     "/ETHUSD.csv.gz",
        //     "/SPX.csv.gz",
        //     "/STOXX50E.csv.gz"
        // };

        // run(filenames, epsilonStart, epsilonStep, epsilonEnd);
    }

    @Test
    public void testWithDatabase() throws Exception {
        double epsilonStart = 0.005;
        double epsilonStep = 0.005;
        double epsilonEnd = 0.05;

        String[][] parameters = {
            { "CalPhase", "Oxygen Optode #754" },
            
            { "Tide Pressure", "Tide Sensor #505" },

            { "C1RPh", "Oxygen Optode #754" },

            { "Pitch", "Doppler Current Profiler Sensor" },

            { "Roll", "Doppler Current Profiler Sensor" },

            { "Max Tilt", "DCPS #122" },
            { "Max Tilt", "DCPS #192" },
            { "Max Tilt", "Doppler Current Profiler Sensor" },

            { "Std Dev Tilt", "Doppler Current Profiler Sensor" },

            { "Turbidity#16340", "Analog Sensors #0" },

            { "InRawTemp", "Conductivity Sensor #43" },

            { "Salinity", "Conductivity Sensor #43" },
            { "Salinity", "Conductivity Sensor #41" },

            { "C2Amp", "Oxygen Optode #754" },

            { "Tide Level", "Tide Sensor #505" },

            { "TCPhase", "Oxygen Optode #754" },

            { "Tilt Direction", "Doppler Current Profiler Sensor" },

            { "Conductivity", "Conductivity Sensor #43" },
            { "Conductivity", "SeaBird SBE #1111" },
            { "Conductivity", "Conductivity Sensor #41" },
            //{ "Conductivity", "SeaBird SBE #2222" }, contains ~150k rows of only 0s

            { "Std Dev Heading", "Doppler Current Profiler Sensor" },

            { "ZAmp", "Conductivity Sensor #43" },

            { "Temperature", "Conductivity Sensor #43" },
            { "Temperature", "Temperature Sensor #1063" },
            { "Temperature", "Conductivity Sensor #41" },
            { "Temperature", "Tide Sensor #505" },
            { "Temperature", "Oxygen Optode #754" },
            { "Temperature", "SeaBird SBE #1111" },
            //{ "Temperature", "SeaBird SBE #2222" }, contains ~150k rows of only 0s
            { "Temperature", "Pressure Sensor #1955" },

            { "Rawdata Temperature", "Temperature Sensor #1063" },

            { "C2RPh", "Oxygen Optode #754" },

            { "Pressure", "SeaBird SBE #1111" },
            { "Pressure", "Tide Sensor #505" },
            //{ "Pressure", "SeaBird SBE #2222" }, contains ~150k rows of only 0s
            { "Pressure", "Pressure Sensor #1955" },

            { "Turbidity#16340 Raw data", "Analog Sensors #0" },

            { "Abs Tilt", "DCPS #122" },
            { "Abs Tilt", "DCPS #192" },
            { "Abs Tilt", "Doppler Current Profiler Sensor" },

            { "Heading", "DCPS #122" },
            { "Heading", "DCPS #192" },
            { "Heading", "Doppler Current Profiler Sensor" },

            { "C1Amp", "Oxygen Optode #754" },

            { "Ping Count", "DCPS #122" },
            { "Ping Count", "DCPS #192" },
            //{ "Ping Count", "Doppler Current Profiler Sensor" }, contains ~19k rows of only 300s

            { "Density", "Conductivity Sensor #43" },
            { "Density", "Conductivity Sensor #41" },

            { "ExRawTemp", "Conductivity Sensor #43" },

            { "UsedRange", "Conductivity Sensor #43" },

            { "AirSaturation", "Oxygen Optode #754" },

            { "Conductance", "Conductivity Sensor #43" },

            { "Record Status", "DCPS #192" },

            { "RawCond0", "Conductivity Sensor #43" },

            { "RawTemp", "Oxygen Optode #754" },

            { "Soundspeed", "Conductivity Sensor #43" },
            { "Soundspeed", "Conductivity Sensor #41" },

            { "O2Concentration", "Oxygen Optode #754" },

            { "Chlorophyll#2103755", "Analog Sensors #0" },

            { "Record State", "DCPS #122" },
            { "Record State", "Doppler Current Profiler Sensor" },

            { "Sound Velocity", "SeaBird SBE #1111" },
            //{ "Sound Velocity", "SeaBird SBE #2222" }, contains ~150k rows of only 0s

            { "Oxygen", "SeaBird SBE #1111" },
            //{ "Oxygen", "SeaBird SBE #2222" } contains ~150k rows of only 0s
        };

        runWithDbDataset(parameters, epsilonStart, epsilonStep, epsilonEnd);
    }
}
